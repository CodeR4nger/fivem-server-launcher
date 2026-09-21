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
        return await OpenCoreAsync(client, path => _processLauncher.StartExecutableAsync(path));
    }

    public async Task<LaunchResult> OpenAsync(FiveMLaunchOptions options)
    {
        var client = options.GameClient ?? GameClient.FiveM;
        return await OpenCoreAsync(client, path => _processLauncher.StartExecutableAsync(path, options.ToCommandLineArgs()));
    }

    private async Task<LaunchResult> OpenCoreAsync(GameClient client, Func<string, Task> start)
    {
        try
        {
            var executablePath = await _installLocator.GetExecutablePathAsync(client);

            if (executablePath is null)
            {
                return new LaunchResult.NotInstalled(client);
            }

            await start(executablePath);
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