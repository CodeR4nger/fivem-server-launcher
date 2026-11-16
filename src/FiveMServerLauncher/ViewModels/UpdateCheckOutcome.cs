namespace FiveMServerLauncher.ViewModels;

/// <summary>
/// What the last on-demand Check for updates said on its Settings row. A found update
/// has no row line: the title-bar pill carries it.
/// </summary>
internal enum UpdateCheckOutcome
{
    None,
    Checking,
    UpToDate,
    Failed
}
