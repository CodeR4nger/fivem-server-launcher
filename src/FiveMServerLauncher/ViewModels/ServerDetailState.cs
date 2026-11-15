namespace FiveMServerLauncher.ViewModels;

/// <summary>What the detail pane can say about a saved server's published data.</summary>
public enum ServerDetailState
{
    /// <summary>In the current catalog: the published values are live.</summary>
    Listed,

    /// <summary>Has a cfx id but is not in the current catalog: last known values are stale.</summary>
    Offline,

    /// <summary>No cfx id: the server was never found in the catalog.</summary>
    NotListed,

    /// <summary>
    /// A loopback server not live in the catalog whose manual connection parameters
    /// (build, pure mode, game) are what a connect applies, so the pane shows those.
    /// </summary>
    Manual
}
