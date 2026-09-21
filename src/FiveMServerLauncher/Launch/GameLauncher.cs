using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Launch;

public class GameLauncher(
    IGameProcessLauncher processLauncher,
    ICitizenFxPreparer citizenFxPreparer,
    IClientInstallLocator installLocator)
{
    private readonly IGameProcessLauncher _processLauncher = processLauncher;
    private readonly ICitizenFxPreparer _citizenFxPreparer = citizenFxPreparer;
    private readonly IClientInstallLocator _installLocator = installLocator;

    public async Task<LaunchResult> OpenAsync(GameClient client)
    {
        try
        {
            var executablePath = await _installLocator.GetExecutablePathAsync(client);

            if (executablePath is null)
            {
                return new LaunchResult.NotInstalled(client);
            }

            await _processLauncher.StartExecutableAsync(executablePath);
        }
        catch
        {
            return new LaunchResult.StartFailed();
        }

        return new LaunchResult.OpenClient(client);
    }

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
            return await OpenAsync(profile.GameClient ?? GameClient.FiveM);
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