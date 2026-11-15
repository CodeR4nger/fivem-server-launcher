using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class ConnectRequestListenerTests : IDisposable
{
    private readonly ConnectRequestListener _listener =
        new($"Local\\tests.connect.{Guid.NewGuid()}");

    private readonly CancellationTokenSource _cts = new();

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Dispose();
    }

    private Task SendLineAsync(string line, int connectTimeoutMs)
    {
        return SendLineAsync(line, connectTimeoutMs, _listener.PipeName);
    }

    private static async Task<string> SendLineAsync(string line, int connectTimeoutMs, string pipeName)
    {
        await using var client = new NamedPipeClientStream(
            ".", pipeName, PipeDirection.Out);
        await client.ConnectAsync(connectTimeoutMs);

        var bytes = Encoding.UTF8.GetBytes(line + "\n");
        await client.WriteAsync(bytes);
        await client.FlushAsync();
        return line;
    }

    [Fact]
    public async Task Start_WhenClientSendsAddress_ShouldInvokeHandlerWithIt()
    {
        // Given
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _listener.Start(address => received.TrySetResult(address), _cts.Token);

        // When
        await SendLineAsync("cfx.re/join/abc", connectTimeoutMs: 2000);

        // Then
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        Assert.Equal("cfx.re/join/abc", received.Task.Result);
    }

    [Fact]
    public async Task Start_WhenSecondClientConnects_ShouldServeItToo()
    {
        // Given — the accept loop continues after each connection.
        var first = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        _listener.Start(address =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                first.TrySetResult(address);
            }
            else
            {
                second.TrySetResult(address);
            }
        }, _cts.Token);
        await SendLineAsync("first", connectTimeoutMs: 2000);
        await first.Task.WaitAsync(TimeSpan.FromSeconds(3));

        // When
        await SendLineAsync("second", connectTimeoutMs: 2000);

        // Then
        var completed = await Task.WhenAny(second.Task, Task.Delay(3000));
        Assert.Same(second.Task, completed);
        Assert.Equal("second", second.Task.Result);
    }

    [Fact]
    public async Task Start_WhenClientDisconnectsWithoutLine_ShouldKeepListening()
    {
        // Given — a client that connects and leaves without saying anything must
        // not kill the loop.
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _listener.Start(address => received.TrySetResult(address), _cts.Token);

        await using (var silent = new NamedPipeClientStream(".", _listener.PipeName, PipeDirection.Out))
        {
            await silent.ConnectAsync(2000);
        }

        // When
        await SendLineAsync("still-here", connectTimeoutMs: 2000);

        // Then
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        Assert.Equal("still-here", received.Task.Result);
    }

    [Fact]
    public async Task Start_WhenClientSendsEmptyLine_ShouldDispatchItVerbatim()
    {
        // Given — the listener transports lines, it does not judge them: an
        // empty line reaches the handler, and the VM's whitespace guard is the
        // decoder.
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _listener.Start(address => received.TrySetResult(address), _cts.Token);

        // When
        await SendLineAsync(string.Empty, connectTimeoutMs: 2000);

        // Then
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        Assert.Equal(string.Empty, received.Task.Result);
    }

    [Fact]
    public async Task Start_WhenCancelled_ShouldStopAccepting()
    {
        // Given
        _listener.Start(_ => { }, _cts.Token);
        await Task.Delay(100);

        // When
        _cts.Cancel();
        await Task.Delay(100);

        // Then — a new client can no longer find a server end.
        await using var client = new NamedPipeClientStream(
            ".", _listener.PipeName, PipeDirection.Out);

        await Assert.ThrowsAsync<TimeoutException>(
            () => client.ConnectAsync(200));
    }

    [Fact]
    public async Task Start_WhenHandlerThrows_ShouldKeepLoopAlive()
    {
        // Given — the handler is third-party code to the listener: one bad
        // address must not take the loop down.
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        _listener.Start(address =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                throw new InvalidOperationException("handler blew up");
            }

            received.TrySetResult(address);
        }, _cts.Token);
        await SendLineAsync("boom", connectTimeoutMs: 2000);
        await Task.Delay(150);

        // When
        await SendLineAsync("survivor", connectTimeoutMs: 2000);

        // Then
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        Assert.Equal("survivor", received.Task.Result);
    }

    [Fact]
    public async Task Start_WhenClientConnectsAndNeverWrites_ShouldDropItAndServeLaterClients()
    {
        // Given: a listener with a short read timeout and a client that connects, holds the
        // connection open and never writes (the single server instance is occupied)
        using var listener = new ConnectRequestListener(
            $"Local\\tests.connect.{Guid.NewGuid()}", readTimeout: TimeSpan.FromMilliseconds(300));
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        listener.Start(address => received.TrySetResult(address), _cts.Token);
        await using var silent = new NamedPipeClientStream(".", listener.PipeName, PipeDirection.Out);
        await silent.ConnectAsync(2000);

        // When: a later client connects while the silent one is still holding on
        await SendLineAsync("after-silent", connectTimeoutMs: 3000, listener.PipeName);

        // Then: the listener gave up on the silent client and served the later one
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        Assert.Equal("after-silent", await received.Task);
    }

    [Fact]
    public async Task Start_WhenLineExceedsCap_ShouldDropItWithoutDispatchAndServeLaterClients()
    {
        // Given
        using var listener = new ConnectRequestListener(
            $"Local\\tests.connect.{Guid.NewGuid()}", maxLineBytes: 16);
        var calls = new List<string>();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        listener.Start(address =>
        {
            lock (calls)
            {
                calls.Add(address);
            }

            if (address == "ok")
            {
                received.TrySetResult(address);
            }
        }, _cts.Token);

        // When: an oversized line (the listener may drop it mid-write, so the sender can see
        // a broken pipe), then a normal one
        try
        {
            await SendLineAsync(new string('x', 100), connectTimeoutMs: 2000, listener.PipeName);
        }
        catch (IOException)
        {
        }

        await SendLineAsync("ok", connectTimeoutMs: 3000, listener.PipeName);

        // Then: only the normal line ever reached the handler
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        lock (calls)
        {
            Assert.Equal(["ok"], calls);
        }
    }

    [Fact]
    public async Task Start_WhenLastLineHasNoNewline_ShouldStillDispatchIt()
    {
        // Given: the transport keeps ReadLine semantics — a final unterminated line counts
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _listener.Start(address => received.TrySetResult(address), _cts.Token);

        // When
        await using (var client = new NamedPipeClientStream(".", _listener.PipeName, PipeDirection.Out))
        {
            await client.ConnectAsync(2000);
            await client.WriteAsync(Encoding.UTF8.GetBytes("cfx.re/join/xyz\r"));
            await client.FlushAsync();
        }

        // Then: delivered, with the carriage return stripped like ReadLine does
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        Assert.Equal("cfx.re/join/xyz", await received.Task);
    }

    [Fact]
    public async Task Start_ShouldGrantThePipeToTheCurrentUserOnly()
    {
        // Given: a listening first instance
        _listener.Start(_ => { }, _cts.Token);
        var currentUser = WindowsIdentity.GetCurrent().User!;

        // When: a client opens the inbound pipe for writing plus reading its security
        // descriptor (asking for read data on an inbound pipe is refused)
        await using var client = new NamedPipeClientStream(
            ".",
            _listener.PipeName,
            PipeAccessRights.WriteData | PipeAccessRights.ReadPermissions,
            PipeOptions.None,
            TokenImpersonationLevel.None,
            HandleInheritability.None);
        await client.ConnectAsync(2000);
        var rules = client.GetAccessControl()
            .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
            .Cast<PipeAccessRule>()
            .ToList();

        // Then: only the current user is granted anything (no Everyone/Anonymous/other users)
        Assert.NotEmpty(rules);
        Assert.All(rules, rule => Assert.Equal(currentUser, rule.IdentityReference));
    }
}
