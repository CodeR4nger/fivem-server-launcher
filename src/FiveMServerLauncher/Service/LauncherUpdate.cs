namespace FiveMServerLauncher.Service;

public sealed record LauncherUpdate(string Tag, string AssetName, long AssetSize, string DownloadUrl);
