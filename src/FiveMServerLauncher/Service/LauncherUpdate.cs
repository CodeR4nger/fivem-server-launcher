namespace FiveMServerLauncher.Service;

// Sha256 is the asset's published digest as lowercase hex; null when the release publishes
// none (the applier then falls back to the size check).
public sealed record LauncherUpdate(
    string Tag,
    string AssetName,
    long AssetSize,
    string DownloadUrl,
    string? Sha256 = null);
