namespace FiveMServerLauncher.Configuration;

/// <summary>
/// The remembered main-window placement, in WPF device-independent units: the normal
/// (restore) bounds plus whether the window was maximized. Startup validates it through
/// <c>Views.WindowPlacementRules</c> before anything is applied.
/// </summary>
public sealed record WindowPlacement(double Left, double Top, double Width, double Height, bool IsMaximized);
