using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Launch;

public class GameLauncher(
    IGameProcessLauncher processLauncher,
    ICitizenFxPreparer citizenFxPreparer)
{
    private readonly IGameProcessLauncher _processLauncher = processLauncher;
    private readonly ICitizenFxPreparer _citizenFxPreparer = citizenFxPreparer;

    public async Task<LaunchResult> ConnectAsync(ServerProfile profile)
    {
        try
        {
            await _citizenFxPreparer.PrimeAsync(profile);
        }
        catch
        {
        }

        var options = FiveMLaunchOptions.FromServerProfile(profile);

        if (options.ToUri() is not { } uri)
        {
            return new LaunchResult.OpenClient(profile.GameClient ?? GameClient.FiveM);
        }

        try
        {
            await _processLauncher.StartAsync(uri);
        }
        catch
        {
            return new LaunchResult.StartFailed();
        }

        return new LaunchResult.Connect(uri);
    }
}