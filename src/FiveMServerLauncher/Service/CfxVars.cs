using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

internal static class CfxVars
{
    /// <summary>
    /// The single CFX-vars → <see cref="CfxServerInfo"/> mapping shared by the
    /// <c>/single/</c> route and the catalog route, so priming never depends on the
    /// address form used to reach a server. <paramref name="requestSteamTicket"/> keeps
    /// its per-route source (a top-level field in <c>/single/</c>, a var in the catalog).
    /// </summary>
    public static CfxServerInfo ToServerInfo(
        string cfxId,
        string? projectName,
        IDictionary<string, string>? variables,
        string? requestSteamTicket)
    {
        return new CfxServerInfo
        {
            CfxId = cfxId,
            ProjectName = projectName ?? string.Empty,
            EnforceGameBuild = TryGetInt(variables, "sv_enforceGameBuild"),
            GameClient = TryGetGameClient(variables),
            PureLevel = TryGetInt(variables, "sv_pureLevel"),
            RequestSteamTicket = MapSteamTicket(requestSteamTicket),
            DefaultGameBuild = TryGetInt(variables, "sv_defaultGameBuild"),
            ReplaceExecutableToSwitchBuilds = TryGetBool(variables, "sv_replaceExeToSwitchBuilds"),
            PoolSizesIncrease = TryGetString(variables, "sv_poolSizesIncrease"),
            SteamEnforced = TryGetBool(variables, "sv_enforceSteamAuth")
        };
    }

    public static string? TryGetIconUrl(IDictionary<string, string>? variables)
    {
        // CFX's icon source since the 2026-10 change: the server-published
        // `icon_url` var (https://cfx-server-metadata.cfx-services.net/images/<guid>).
        // The URL is validated at the download seam; here it is only read.
        var url = TryGetString(variables, "icon_url");

        return string.IsNullOrWhiteSpace(url) ? null : url;
    }

    public static GameClient? TryGetGameClient(IDictionary<string, string>? variables)
    {
        var game = GetVariable(variables, "gamename");
        return game is null
            ? null
            : MapGameClient(game);
    }

    public static GameClient? MapGameClient(string game)
    {
        return game switch
        {
            "gta5" => GameClient.FiveM,
            "gta5enhanced" => GameClient.FiveMEnhanced,
            "rdr3" => GameClient.RedM,
            _ => null
        };
    }

    public static int? TryGetInt(IDictionary<string, string>? variables, string name)
    {
        var value = GetVariable(variables, name);
        return value is not null && int.TryParse(value, out var result)
            ? result
            : null;
    }

    public static bool? TryGetBool(IDictionary<string, string>? variables, string name)
    {
        var value = GetVariable(variables, name);
        return value switch
        {
            "true" => true,
            "false" => false,
            _ => null,
        };
    }

    public static string? TryGetString(IDictionary<string, string>? variables, string name)
    {
        return GetVariable(variables, name);
    }

    private static string? GetVariable(IDictionary<string, string>? variables, string name)
    {
        return variables is not null && variables.TryGetValue(name, out var value)
            ? value
            : null;
    }

    public static bool? MapSteamTicket(string? value)
    {
        return value switch
        {
            "on" => true,
            "off" => false,
            _ => null
        };
    }
}