using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Configuration;

public class LauncherSettings
{
    public GameClient PreferredClient { get; set; } = GameClient.FiveM;
    public bool AutoLaunch { get; set; } = false;
    public bool WebLinks { get; set; } = false;
    public bool MinimizeToTray { get; set; } = false;
    public InterfaceAnimations InterfaceAnimations { get; set; }
    // Main-window size, position and maximized state (null = never saved: default size, centred).
    public WindowPlacement? Window { get; set; }
    public string? LastServerAddress { get; set; }
    public string? Language { get; set; }
    public int? DevGameBuild { get; set; }
    public int? DevPureMode { get; set; }
}
