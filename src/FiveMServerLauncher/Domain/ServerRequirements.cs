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
            SteamRequired = SteamRequiredFor(published.SteamRequired, savedServer?.RequiresSteam),
            DiscordRequired = savedServer?.RequiresDiscord
        };
    }

    /// <summary>
    /// The one Steam merge rule: a manual <c>true</c> is additive (always requires
    /// Steam, even over a published <c>false</c>); otherwise the published value wins
    /// when present, else the manual fallback.
    /// </summary>
    public static bool? SteamRequiredFor(bool? published, bool? manual)
    {
        return manual == true ? true : published ?? manual;
    }
}