namespace FiveMServerLauncher.Service;

public class CfxServerInfo
{
    public string CfxId { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;

    public int? EnforceGameBuild { get; init; }
    public int? PureLevel { get; init; }
    public bool? RequestSteamTicket { get; init; }
}
