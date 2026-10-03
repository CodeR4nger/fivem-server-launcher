using System.Diagnostics;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class ConnectRequestForwarderTests : IDisposable
{
    private readonly ConnectRequestListener _listener =
        new($"Local\\tests.connect.{Guid.NewGuid()}");

    private readonly CancellationTokenSource _cts = new();

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Dispose();
    }

    [Fact]
    public async Task TryForward_WhenListenerRunning_ShouldDeliverAddressAndReportTrue()
    {
        // Given — a first instance is listening on the real pipe.
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _listener.Start(address => received.TrySetResult(address), _cts.Token);
        var forwarder = new ConnectRequestForwarder(_listener.PipeName);

        // When
        var delivered = forwarder.TryForward("cfx.re/join/abc", timeoutMs: 2000);

        // Then
        Assert.True(delivered);
        var completed = await Task.WhenAny(received.Task, Task.Delay(3000));
        Assert.Same(received.Task, completed);
        Assert.Equal("cfx.re/join/abc", received.Task.Result);
    }

    [Fact]
    public void TryForward_WhenNoListener_ShouldReturnFalseWithinTimeout()
    {
        // Given — no first instance on this private pipe: the duplicate exits
        // silently, and the failure must resolve inside the timeout budget.
        var forwarder = new ConnectRequestForwarder($"Local\\tests.connect.{Guid.NewGuid()}");
        var stopwatch = Stopwatch.StartNew();

        // When
        var delivered = forwarder.TryForward("cfx.re/join/abc", timeoutMs: 200);

        // Then
        stopwatch.Stop();
        Assert.False(delivered);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(1));
    }
}
