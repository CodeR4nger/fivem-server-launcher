using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Launch;

public sealed class LaunchResult
{
    public Uri? ConnectUri { get; }
    public GameClient? GameClient { get; }

    private LaunchResult(Uri? connectUri, GameClient? gameClient)
    {
        ConnectUri = connectUri;
        GameClient = gameClient;
    }

    public static LaunchResult Connect(Uri connectUri)
    {
        return new LaunchResult(connectUri, null);
    }

    public static LaunchResult OpenClient(GameClient gameClient)
    {
        return new LaunchResult(null, gameClient);
    }
}