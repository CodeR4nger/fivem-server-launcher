namespace FiveMServerLauncher.ViewModels;

/// <summary>The Servers page's filter chips over the saved list.</summary>
public enum SavedServerFilter
{
    All,

    /// <summary>Live in the current catalog.</summary>
    Online,

    /// <summary>Every row that is not online: offline, not listed and manual rows alike.</summary>
    Offline
}
