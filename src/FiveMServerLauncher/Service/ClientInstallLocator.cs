using FiveMServerLauncher.Core.Enums;
using System.IO;

namespace FiveMServerLauncher.Service;

public sealed class ClientInstallLocator : IClientInstallLocator
{
    public Task<bool> IsInstalledAsync(GameClient client)
    {
        var path = GetPath(client);
        return Task.FromResult(path is not null && Path.Exists(path));
    }

    public Task<string?> GetExecutablePathAsync(GameClient client)
    {
        return Task.FromResult(GetPath(client));
    }

    private static string? GetPath(GameClient client)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return client switch
        {
            GameClient.FiveM => Path.Combine(localAppData, "FiveM", "FiveM.app", "FiveM.exe"),
            GameClient.FiveMEnhanced => Path.Combine(localAppData, "FiveM for GTAV Enhanced", "FiveM.app", "FiveM.exe"),
            GameClient.RedM => Path.Combine(localAppData, "RedM", "RedM.app", "RedM.exe"),
            _ => null,
        };
    }
}