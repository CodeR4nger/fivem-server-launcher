using System.IO.Pipes;
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
}
