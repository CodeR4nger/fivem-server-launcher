using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

internal static class CfxVars
{
    public static string? TryGetIconVersion(Master.ServerData data)
    {
        // The catalog's iconVersion is a typed proto field, never a var. Any non-zero
        // value — including negative — means the server has an icon at that version
        // (verified live: negative-version icon URLs return 200 + PNG); only zero
        // (field absent) means no icon. A version that unexpectedly 404s degrades to
        // a null icon in the download step.
        return data.IconVersion != 0
            ? data.IconVersion.ToString()
            : null;
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