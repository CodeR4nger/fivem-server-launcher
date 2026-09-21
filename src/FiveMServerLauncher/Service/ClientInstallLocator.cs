using System.IO;
using FiveMServerLauncher.Core.Enums;
using Microsoft.Win32;

namespace FiveMServerLauncher.Service;

public sealed class ClientInstallLocator : IClientInstallLocator
{
    private const string ExecutableName = "FiveM.exe";
    private const string RegistryKeyPath = @"HKEY_CURRENT_USER\Software\CitizenFX\FiveM";
    private const string RegistryValueName = "Last Run Location";

    private readonly Func<string, bool> _exists;
    private readonly Func<string?> _legacyInstallDirectoryProvider;

    public ClientInstallLocator() : this(File.Exists, ReadLastRunLocation)
    {
    }

    public ClientInstallLocator(Func<string, bool> exists, Func<string?> legacyInstallDirectoryProvider)
    {
        _exists = exists;
        _legacyInstallDirectoryProvider = legacyInstallDirectoryProvider;
    }

    public Task<bool> IsInstalledAsync(GameClient client)
    {
        return Task.FromResult(Resolve(client).InstallDirectory is not null);
    }

    public Task<string?> GetExecutablePathAsync(GameClient client)
    {
        return Task.FromResult<string?>(Resolve(client).ExecutablePath);
    }

    public Task<string?> GetInstallDirectoryAsync(GameClient client)
    {
        return Task.FromResult<string?>(Resolve(client).InstallDirectory);
    }

    private (string? InstallDirectory, string? ExecutablePath) Resolve(GameClient client)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return client switch
        {
            GameClient.FiveM => ResolveLegacy(localAppData),
            GameClient.FiveMEnhanced => ResolveEnhanced(localAppData),
            _ => (null, null),
        };
    }

    private (string?, string?) ResolveLegacy(string localAppData)
    {
        var registered = _legacyInstallDirectoryProvider();
        var installDirectory = string.IsNullOrWhiteSpace(registered)
            ? Path.Combine(localAppData, "FiveM", "FiveM.app")
            : NormalizeDirectory(registered);

        var insideApp = Path.Combine(installDirectory, ExecutableName);
        var sibling = Path.Combine(Path.GetDirectoryName(installDirectory) ?? installDirectory, ExecutableName);
        var executable = _exists(insideApp) ? insideApp : _exists(sibling) ? sibling : null;
        return executable is null ? (null, null) : (installDirectory, executable);
    }

    private (string?, string?) ResolveEnhanced(string localAppData)
    {
        var installDirectory = Path.Combine(localAppData, "FiveM for GTAV Enhanced");
        var executable = Path.Combine(installDirectory, ExecutableName);
        return _exists(executable) ? (installDirectory, executable) : (null, null);
    }

    private static string NormalizeDirectory(string directory)
    {
        return directory.Trim().TrimEnd('\\');
    }

    private static string? ReadLastRunLocation()
    {
        var value = Registry.GetValue(RegistryKeyPath, RegistryValueName, null) as string;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}