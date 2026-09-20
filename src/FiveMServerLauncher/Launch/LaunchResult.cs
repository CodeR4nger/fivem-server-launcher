using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Launch;

public abstract record LaunchResult
{
    private LaunchResult()
    {
    }

    public sealed record Connect(Uri ConnectUri) : LaunchResult;

    public sealed record OpenClient(GameClient GameClient) : LaunchResult;
}