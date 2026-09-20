using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Launch;

public class GameLauncher(IGameProcessLauncher processLauncher)
{
    private readonly IGameProcessLauncher _processLauncher = processLauncher;

    public async Task<LaunchResult> ConnectAsync(ServerProfile profile)
    {
        var options = FiveMLaunchOptions.FromServerProfile(profile);

        if (options.ToUri() is not { } uri)
        {
            return LaunchResult.OpenClient(profile.GameClient ?? GameClient.FiveM);
        }

        await _processLauncher.StartAsync(uri);

        return LaunchResult.Connect(uri);
    }
}