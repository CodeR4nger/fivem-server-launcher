using System.Windows.Threading;
using Xunit;

namespace FiveMServerLauncher.Tests.Views;

// A WPF window still alive when its STA thread exits is destroyed by Windows during thread
// teardown, which calls its managed window procedure after the runtime has torn the thread
// down: the CLR asserts and the test host dies, silently ending the run. StaThread.Run must
// therefore shut the thread's dispatcher down (destroying every HWND it owns) before the
// thread ends — whatever the work left open, such as a ContextMenu's popup window.
public class StaThreadTests
{
    [Fact]
    public void Run_ShouldShutDownTheThreadsDispatcherBeforeReturning()
    {
        // Given / When
        var dispatcher = StaThread.Run(() => Dispatcher.CurrentDispatcher);

        // Then
        Assert.True(dispatcher.HasShutdownFinished);
    }

    // WPF's pack:// resource package (every BAML load: Theme.xaml, the windows) is
    // process-global and not thread-safe: two STA threads loading markup at once corrupt
    // its requested-streams list, and every later load in the process then throws a
    // NullReferenceException. xUnit runs test classes in parallel, so Run serializes the
    // WPF work of every caller.
    [Fact]
    public async Task Run_FromConcurrentCallers_ShouldNeverOverlapTheirWork()
    {
        // Given
        var active = 0;
        var mostAtOnce = 0;

        int Work()
        {
            var now = Interlocked.Increment(ref active);
            InterlockedMax(ref mostAtOnce, now);
            Thread.Sleep(200);
            Interlocked.Decrement(ref active);
            return 0;
        }

        // When
        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Task.Run(() => StaThread.Run(Work))));

        // Then
        Assert.Equal(1, mostAtOnce);
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;

        while ((current = Volatile.Read(ref target)) < value
               && Interlocked.CompareExchange(ref target, value, current) != current)
        {
        }
    }

    // A stuck STA test must fail by name instead of stalling the whole run (a hung run
    // reports nothing until vstest gives up), and must not hold the gate forever.
    [Fact]
    public void Run_WhenTheWorkNeverFinishes_ShouldFailWithATimeoutAndReleaseTheGate()
    {
        // Given work that blocks until released
        using var release = new ManualResetEventSlim();

        // When
        var failure = Assert.Throws<TimeoutException>(() =>
            StaThread.Run(() => release.Wait(Timeout.Infinite), TimeSpan.FromMilliseconds(200)));
        var next = StaThread.Run(() => 42);
        release.Set();

        // Then
        Assert.Contains("did not finish", failure.Message);
        Assert.Equal(42, next);
    }

    [Fact]
    public void Run_WhenTheWorkThrows_ShouldStillShutDownTheDispatcher()
    {
        // Given
        Dispatcher? dispatcher = null;

        // When
        var failure = Assert.Throws<InvalidOperationException>(() => StaThread.Run<int>(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            throw new FormatException("boom");
        }));

        // Then
        Assert.IsType<FormatException>(failure.InnerException);
        Assert.True(dispatcher!.HasShutdownFinished);
    }
}
