using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

public class CfxServerInfo
{
    public string CfxId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public GameClient? GameClient { get; init; }

    public int? EnforceGameBuild { get; init; }
    public int? PureLevel { get; init; }
    public bool? RequestSteamTicket { get; init; }
}
