using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Configuration;

public class LauncherSettings
{
    public GamePlatform Platform { get; set; } = GamePlatform.Steam;
    public GameClient PreferredClient { get; set; } = GameClient.FiveM;
    public bool AutoLaunch { get; set; } = false;
    public int ServerPort { get; set; } = 30120;
}
