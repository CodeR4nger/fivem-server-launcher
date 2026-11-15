namespace FiveMServerLauncher.Core;

public static class ConnectArgument
{
    private const string ConnectFlag = "--connect";

    /// <summary>
    /// The address of the last connect intent: <c>--connect &lt;address&gt;</c> or a
    /// <c>cfxlauncher://connect/</c> web link. A trailing <c>--connect</c> with no value
    /// cancels earlier intents; a rejected web link is ignored like any unknown argument.
    /// </summary>
    public static string? TryParse(string[] args)
    {
        string? address = null;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == ConnectFlag)
            {
                address = i + 1 < args.Length ? args[i + 1] : null;
            }
            else if (LauncherUri.TryGetConnectAddress(args[i]) is { } uriAddress)
            {
                address = uriAddress;
            }
        }

        return address;
    }
}
