using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Domain;

public class ServerProfile
{
    public string CfxId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public GameClient? GameClient { get; set; }
    public ServerRequirements Requirements { get; set; } = new();
}