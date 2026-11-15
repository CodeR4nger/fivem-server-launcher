using System.Windows;
using FiveMServerLauncher.Configuration;

namespace FiveMServerLauncher.Views;

/// <summary>The work areas (monitor bounds minus taskbars) of every current monitor, in DIPs.</summary>
public interface IMonitorWorkAreas
{
    IReadOnlyList<Rect> GetWorkAreas();
}

/// <summary>What startup applies: a size, a position (null = centre on screen) and the state.</summary>
public sealed record RestoredPlacement(double Width, double Height, Point? Position, bool IsMaximized);

/// <summary>
/// The window-placement decisions: what a stored placement restores to, and what a window
/// in a given state saves. A position is kept only while enough of the title bar sits on a
/// current monitor to grab it; otherwise (unplugged monitor, changed layout) it centres.
/// </summary>
public sealed class WindowPlacementRules(IMonitorWorkAreas monitors)
{
    public const double MinimumWidth = 1000;
    public const double MinimumHeight = 640;
    public const double DefaultWidth = MinimumWidth;
    public const double DefaultHeight = MinimumHeight;

    // The title-bar strip that must stay reachable: at least this much of it on one monitor.
    private const double CaptionHeight = 40;
    private const double MinimumReachableCaptionWidth = 100;
    private const double MinimumReachableCaptionHeight = 20;

    public RestoredPlacement Restore(WindowPlacement? stored)
    {
        if (stored is null)
        {
            return new RestoredPlacement(DefaultWidth, DefaultHeight, null, false);
        }

        var width = Size(stored.Width, DefaultWidth, MinimumWidth);
        var height = Size(stored.Height, DefaultHeight, MinimumHeight);
        Point? position = double.IsFinite(stored.Left)
                          && double.IsFinite(stored.Top)
                          && IsCaptionReachable(new Rect(stored.Left, stored.Top, width, CaptionHeight))
            ? new Point(stored.Left, stored.Top)
            : null;

        return new RestoredPlacement(width, height, position, stored.IsMaximized);
    }

    /// <summary>
    /// The placement to save: the normal bounds (a maximized or minimized window reports
    /// them as its restore bounds) and whether it is, or restores to, maximized.
    /// </summary>
    public static WindowPlacement Capture(
        WindowState state, WindowState restoreState, Rect currentBounds, Rect restoreBounds)
    {
        var normal = state == WindowState.Normal || restoreBounds.IsEmpty ? currentBounds : restoreBounds;
        var maximized = state == WindowState.Maximized
                        || (state == WindowState.Minimized && restoreState == WindowState.Maximized);

        return new WindowPlacement(normal.Left, normal.Top, normal.Width, normal.Height, maximized);
    }

    private static double Size(double stored, double fallback, double minimum) =>
        double.IsFinite(stored) ? Math.Max(stored, minimum) : fallback;

    private bool IsCaptionReachable(Rect caption) =>
        monitors.GetWorkAreas().Any(area =>
        {
            var visible = Rect.Intersect(area, caption);
            return !visible.IsEmpty
                   && visible.Width >= MinimumReachableCaptionWidth
                   && visible.Height >= MinimumReachableCaptionHeight;
        });
}
