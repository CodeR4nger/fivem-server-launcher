namespace FiveMServerLauncher.Domain;

public class ServerRequirements
{
    public int? GameBuild { get; init; }
    public int? PureMode { get; init; }
    public bool? RequestSteamTicket { get; init; }
}