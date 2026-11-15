using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// The enrichment loop re-downloads the whole catalog whenever its TTL lapses; while the
// window is away (minimized or inactive) nobody can see the rows, so no cycle runs.
public partial class MainViewModelTests
{
    // Long enough for a runaway loop to cycle many times on fake, instant seams.
    private static readonly TimeSpan ParkedProbe = TimeSpan.FromMilliseconds(150);

    [Fact]
    public async Task RunEnrichmentLoopAsync_WhileWindowAway_ShouldNotRefresh()
    {
        // Given — the window is already away when the loop starts
        var enrichment = new FakeServerEnrichmentService();
        var cfxStatus = new FakeCfxStatusService();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), enrichment: enrichment, cfxStatus: cfxStatus);
        vm.SetWindowAttended(false);
        using var cts = new CancellationTokenSource();
        var cycles = new LoopCycleSignal(target: 1);

        // When
        var loop = vm.RunEnrichmentLoopAsync(cts.Token, TimeSpan.FromMinutes(1), cycles.DelayAsync);
        await Task.Delay(ParkedProbe);

        // Then — parked: no catalog refresh, no status refresh
        Assert.Equal(0, enrichment.RefreshCalls);
        Assert.Equal(0, cfxStatus.GetCalls);
        cts.Cancel();
        await loop;
    }

    [Fact]
    public async Task RunEnrichmentLoopAsync_WhenWindowReturns_ShouldRefreshAtOnce()
    {
        // Given — a loop parked while away
        var enrichment = new FakeServerEnrichmentService();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), enrichment: enrichment);
        vm.SetWindowAttended(false);
        using var cts = new CancellationTokenSource();
        var cycles = new LoopCycleSignal(target: 1);
        var loop = vm.RunEnrichmentLoopAsync(cts.Token, TimeSpan.FromMinutes(1), cycles.DelayAsync);
        await Task.Delay(ParkedProbe);

        // When — the window is restored/activated
        vm.SetWindowAttended(true);
        await cycles.ReachedAsync();

        // Then — the refresh-on-restore ran without waiting for a cadence tick
        Assert.Equal(1, enrichment.RefreshCalls);
        cts.Cancel();
        await loop;
    }

    [Fact]
    public async Task RunEnrichmentLoopAsync_WhenWindowLeavesBetweenCycles_ShouldParkBeforeTheNextCycle()
    {
        // Given — the window goes away during the cadence wait after the first cycle
        var enrichment = new FakeServerEnrichmentService();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), enrichment: enrichment);
        using var cts = new CancellationTokenSource();
        var waits = 0;

        async Task LeaveDuringWait(TimeSpan duration, CancellationToken token)
        {
            Interlocked.Increment(ref waits);
            vm.SetWindowAttended(false);
            await Task.Yield();
        }

        // When
        var loop = vm.RunEnrichmentLoopAsync(cts.Token, TimeSpan.FromMinutes(1), LeaveDuringWait);
        await Task.Delay(ParkedProbe);

        // Then — exactly one cycle, then parked
        Assert.Equal(1, enrichment.RefreshCalls);
        Assert.Equal(1, Volatile.Read(ref waits));
        cts.Cancel();
        await loop;
    }

    [Fact]
    public async Task RunEnrichmentLoopAsync_WhenCancelledWhileParked_ShouldEnd()
    {
        // Given
        var enrichment = new FakeServerEnrichmentService();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), enrichment: enrichment);
        vm.SetWindowAttended(false);
        using var cts = new CancellationTokenSource();
        var loop = vm.RunEnrichmentLoopAsync(cts.Token, TimeSpan.FromMinutes(1));

        // When — the window closes while minimized
        cts.Cancel();

        // Then
        await loop.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(0, enrichment.RefreshCalls);
    }

    [Fact]
    public async Task RefreshServersCommand_WhileWindowAway_ShouldStillForceRefresh()
    {
        // Given — the manual ↻ is an explicit request and is never paused
        var enrichment = new FakeServerEnrichmentService();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), enrichment: enrichment);
        vm.SetWindowAttended(false);

        // When
        await vm.RefreshServerInfoAsync(forceRefresh: true);

        // Then
        Assert.Equal(1, enrichment.ForcedRefreshCalls);
    }
}
