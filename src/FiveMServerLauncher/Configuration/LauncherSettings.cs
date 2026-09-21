using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Configuration;

public class LauncherSettings
{
    public GameClient PreferredClient { get; set; } = GameClient.FiveM;
    public bool AutoLaunch { get; set; } = false;
    public string? LastServerAddress { get; set; }
}
