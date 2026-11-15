using System.Windows;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class WindowPlacementRulesTests
{
    // A 1920×1080 primary monitor (taskbar 40 px) and a second one to its left.
    private static readonly Rect Primary = new(0, 0, 1920, 1040);
    private static readonly Rect LeftMonitor = new(-1280, 0, 1280, 984);

    private static WindowPlacementRules Rules(params Rect[] workAreas) =>
        new(new FakeMonitorWorkAreas(workAreas));

    [Fact]
    public void Restore_WhenNothingIsStored_ShouldUseTheDefaultSizeCentred()
    {
        // Given / When
        var restored = Rules(Primary).Restore(null);

        // Then
        Assert.Equal(new RestoredPlacement(1000, 640, null, false), restored);
    }

    [Fact]
    public void Restore_WhenStoredOnAMonitor_ShouldUseTheStoredBounds()
    {
        // Given
        var stored = new WindowPlacement(120, 80, 1300, 800, false);

        // When
        var restored = Rules(Primary).Restore(stored);

        // Then
        Assert.Equal(new RestoredPlacement(1300, 800, new Point(120, 80), false), restored);
    }

    [Fact]
    public void Restore_WhenStoredOnASecondMonitor_ShouldKeepItThere()
    {
        // Given — negative coordinates on the monitor left of the primary
        var stored = new WindowPlacement(-1200, 50, 1000, 640, false);

        // When
        var restored = Rules(Primary, LeftMonitor).Restore(stored);

        // Then
        Assert.Equal(new Point(-1200, 50), restored.Position);
    }

    [Fact]
    public void Restore_WhenStoredOffEveryMonitor_ShouldKeepTheSizeButCentre()
    {
        // Given — saved on the left monitor, which is unplugged now
        var stored = new WindowPlacement(-1200, 50, 1100, 700, true);

        // When
        var restored = Rules(Primary).Restore(stored);

        // Then
        Assert.Equal(new RestoredPlacement(1100, 700, null, true), restored);
    }

    [Theory]
    [InlineData(1870, 100)]   // only 50 px of the title bar left on the right edge
    [InlineData(-950, 100)]   // only 50 px left on the left edge
    [InlineData(300, 1030)]   // title bar below the work area (under the taskbar)
    [InlineData(300, -30)]    // title bar above the top edge: 10 px of it visible
    public void Restore_WhenTooLittleOfTheTitleBarIsReachable_ShouldCentre(double left, double top)
    {
        // Given
        var stored = new WindowPlacement(left, top, 1000, 640, false);

        // When
        var restored = Rules(Primary).Restore(stored);

        // Then
        Assert.Null(restored.Position);
    }

    [Fact]
    public void Restore_WhenEnoughOfTheTitleBarIsReachable_ShouldKeepThePosition()
    {
        // Given — mostly off the right edge, but 120 px of title bar can still be grabbed
        var stored = new WindowPlacement(1800, 100, 1000, 640, false);

        // When
        var restored = Rules(Primary).Restore(stored);

        // Then
        Assert.Equal(new Point(1800, 100), restored.Position);
    }

    [Fact]
    public void Restore_WhenStoredBelowTheMinimum_ShouldClampToTheMinimumSize()
    {
        // Given
        var stored = new WindowPlacement(100, 100, 600, 300, false);

        // When
        var restored = Rules(Primary).Restore(stored);

        // Then
        Assert.Equal(1000, restored.Width);
        Assert.Equal(640, restored.Height);
    }

    [Theory]
    [InlineData(double.NaN, 100, 1200, 700)]
    [InlineData(100, double.PositiveInfinity, 1200, 700)]
    [InlineData(100, 100, double.NaN, 700)]
    [InlineData(100, 100, 1200, double.NegativeInfinity)]
    public void Restore_WhenAStoredValueIsNotFinite_ShouldFallBackForIt(
        double left, double top, double width, double height)
    {
        // Given
        var stored = new WindowPlacement(left, top, width, height, false);

        // When
        var restored = Rules(Primary).Restore(stored);

        // Then — a broken size falls back to the default, a broken position centres
        Assert.True(double.IsFinite(restored.Width) && restored.Width >= 1000);
        Assert.True(double.IsFinite(restored.Height) && restored.Height >= 640);
        Assert.Equal(double.IsFinite(left) && double.IsFinite(top), restored.Position is not null);
    }

    [Fact]
    public void Restore_WhenNoMonitorIsReported_ShouldCentre()
    {
        // Given
        var stored = new WindowPlacement(100, 100, 1000, 640, false);

        // When
        var restored = Rules().Restore(stored);

        // Then
        Assert.Null(restored.Position);
    }

    [Fact]
    public void Capture_WhenNormal_ShouldTakeTheCurrentBounds()
    {
        // Given / When
        var placement = WindowPlacementRules.Capture(
            WindowState.Normal, WindowState.Normal, new Rect(10, 20, 1100, 700), new Rect(0, 0, 1, 1));

        // Then
        Assert.Equal(new WindowPlacement(10, 20, 1100, 700, false), placement);
    }

    [Fact]
    public void Capture_WhenMaximized_ShouldTakeTheRestoreBoundsAndRememberMaximized()
    {
        // Given / When
        var placement = WindowPlacementRules.Capture(
            WindowState.Maximized, WindowState.Maximized, new Rect(-8, -8, 1936, 1056), new Rect(10, 20, 1100, 700));

        // Then
        Assert.Equal(new WindowPlacement(10, 20, 1100, 700, true), placement);
    }

    [Theory]
    [InlineData(WindowState.Maximized, true)]
    [InlineData(WindowState.Normal, false)]
    public void Capture_WhenMinimized_ShouldRememberTheStateItRestoresTo(WindowState restoreState, bool maximized)
    {
        // Given / When
        var placement = WindowPlacementRules.Capture(
            WindowState.Minimized, restoreState, new Rect(-32000, -32000, 160, 28), new Rect(10, 20, 1100, 700));

        // Then
        Assert.Equal(new WindowPlacement(10, 20, 1100, 700, maximized), placement);
    }

    [Fact]
    public void Capture_WhenNotNormalAndNoRestoreBoundsYet_ShouldTakeTheCurrentBounds()
    {
        // Given / When — WPF reports Rect.Empty before the window was ever shown
        var placement = WindowPlacementRules.Capture(
            WindowState.Maximized, WindowState.Maximized, new Rect(10, 20, 1100, 700), Rect.Empty);

        // Then
        Assert.Equal(new WindowPlacement(10, 20, 1100, 700, true), placement);
    }

    private sealed class FakeMonitorWorkAreas(IReadOnlyList<Rect> workAreas) : IMonitorWorkAreas
    {
        public IReadOnlyList<Rect> GetWorkAreas() => workAreas;
    }
}
