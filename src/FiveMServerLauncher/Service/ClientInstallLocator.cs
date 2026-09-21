using FiveMServerLauncher.Core.Enums;
using System.IO;

namespace FiveMServerLauncher.Service;

public sealed class ClientInstallLocator : IClientInstallLocator
{
    private const string ExecutableName = "FiveM.exe";

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
        return Task.FromResult(GetInstalledDirectory(client) is not null);
    }

    public Task<string?> GetExecutablePathAsync(GameClient client)
    {
        var directory = GetInstalledDirectory(client);
        var path = directory is null ? null : Path.Combine(directory, ExecutableName);
        return Task.FromResult(path);
    }

    public Task<string?> GetInstallDirectoryAsync(GameClient client)
    {
        return Task.FromResult(GetInstalledDirectory(client));
    }

    private string? GetInstalledDirectory(GameClient client)
    {
        var directory = GetInstallDirectory(client);
        return directory is not null && _exists(Path.Combine(directory, ExecutableName))
            ? directory
            : null;
    }

    private static string? GetInstallDirectory(GameClient client)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return client switch
        {
            GameClient.FiveM => Path.Combine(localAppData, "FiveM", "FiveM.app"),
            GameClient.FiveMEnhanced => Path.Combine(localAppData, "FiveM for GTAV Enhanced", "FiveM.app"),
            _ => null,
        };
    }
}