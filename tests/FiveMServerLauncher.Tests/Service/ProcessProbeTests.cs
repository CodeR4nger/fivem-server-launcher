using System.Diagnostics;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class ProcessProbeTests
{
    [Fact]
    public void AnyRunning_WhenFetchReturnsProcesses_ShouldReleaseEveryInstance()
    {
        // Given — GetProcessesByName allocates one Process (native handle) per match,
        // and the readiness probe polls repeatedly while preparing an external app, so
        // every returned instance must be released exactly once.
        var first = new Process();
        var second = new Process();
        var released = new List<Process>();

        // When
        var result = ProcessProbe.AnyRunning("steam", _ => [first, second], released.Add);

        // Then
        Assert.True(result);
        Assert.Equal(new Process[] { first, second }, released);
    }

    [Fact]
    public void AnyRunning_WhenFetchReturnsEmpty_ShouldReturnFalseWithoutReleases()
    {
        // Given
        var released = new List<Process>();

        // When
        var result = ProcessProbe.AnyRunning("steam", _ => [], released.Add);

        // Then
        Assert.False(result);
        Assert.Empty(released);
    }

    [Fact]
    public void AnyRunning_WhenFetchThrows_ShouldPropagateWithoutReleasingAnything()
    {
        // Given — the fetcher owns its failure; the probe never sees instances to leak.
        var released = new List<Process>();

        // When / Then
        Assert.Throws<InvalidOperationException>(() =>
            ProcessProbe.AnyRunning("steam", _ => throw new InvalidOperationException("probe failure"), released.Add));
        Assert.Empty(released);
    }
}
