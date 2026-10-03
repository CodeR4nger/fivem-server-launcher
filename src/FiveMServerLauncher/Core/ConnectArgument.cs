namespace FiveMServerLauncher.Core;

public static class ConnectArgument
{
    public static string? TryParse(string[] args)
    {
        var lastIndex = Array.LastIndexOf(args, "--connect");

        return lastIndex >= 0 && lastIndex + 1 < args.Length ? args[lastIndex + 1] : null;
    }
}
