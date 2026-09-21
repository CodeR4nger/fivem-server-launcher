namespace FiveMServerLauncher.Domain;

public sealed record ServerRequirements
{
    public int? GameBuild { get; init; }
    public int? PureMode { get; init; }
    public bool? RequestSteamTicket { get; init; }
    public int? DefaultBuild { get; init; }
    public bool? ReplaceExecutable { get; init; }
    public string? PoolSizesIncrease { get; init; }

    public bool? SteamRequired { get; init; }
    public bool? DiscordRequired { get; init; }

    public static ServerRequirements ForConnection(ServerRequirements published, SavedServer? savedServer)
    {
        return published with
        {
            SteamRequired = published.SteamRequired ?? savedServer?.RequiresSteam,
            DiscordRequired = savedServer?.RequiresDiscord
        };
    }
}