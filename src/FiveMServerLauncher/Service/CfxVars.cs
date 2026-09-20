using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

internal static class CfxVars
{
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
        if (variables is null ||
            !variables.TryGetValue(name, out var value))
        {
            return null;
        }

        return int.TryParse(value, out var result)
            ? result
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