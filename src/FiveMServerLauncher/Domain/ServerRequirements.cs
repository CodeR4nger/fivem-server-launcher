namespace FiveMServerLauncher.Domain;

public class ServerRequirements
{
    public int? GameBuild { get; init; }
    public int? PureMode { get; init; }
    public bool? RequestSteamTicket { get; init; }
    public int? DefaultBuild { get; init; }
    public bool? ReplaceExecutable { get; init; }
    public string? PoolSizesIncrease { get; init; }
}