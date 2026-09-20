using FiveMServerLauncher.Core.Enums;
using System.IO;

namespace FiveMServerLauncher.Service;

public sealed class ClientInstallLocator : IClientInstallLocator
{
    private readonly Func<string, bool> _exists;

    public ClientInstallLocator() : this(File.Exists)
    {
    }

    public ClientInstallLocator(Func<string, bool> exists)
    {
        _exists = exists;
    }

    public Task<bool> IsInstalledAsync(GameClient client)
    {
        var path = GetPath(client);
        return Task.FromResult(path is not null && _exists(path));
    }

    public Task<string?> GetExecutablePathAsync(GameClient client)
    {
        var path = GetPath(client);
        return Task.FromResult(path is not null && _exists(path) ? path : null);
    }

    private static string? GetPath(GameClient client)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return client switch
        {
            GameClient.FiveM => Path.Combine(localAppData, "FiveM", "FiveM.app", "FiveM.exe"),
            GameClient.FiveMEnhanced => Path.Combine(localAppData, "FiveM for GTAV Enhanced", "FiveM.app", "FiveM.exe"),
            _ => null,
        };
    }
}