using System.IO;
using FiveMServerLauncher.Core.Enums;
using Microsoft.Win32;

namespace FiveMServerLauncher.Service;

public sealed class ClientInstallLocator : IClientInstallLocator
{
    private const string ExecutableName = "FiveM.exe";
    private const string RedMExecutableName = "RedM.exe";
    private const string RegistryKeyPath = @"HKEY_CURRENT_USER\Software\CitizenFX\FiveM";
    private const string RedMRegistryKeyPath = @"HKEY_CURRENT_USER\Software\CitizenFX\RedM";
    private const string RegistryValueName = "Last Run Location";

    private readonly Func<string, bool> _exists;
    private readonly Func<string?> _legacyInstallDirectoryProvider;
    private readonly Func<string?> _redmInstallDirectoryProvider;

    public ClientInstallLocator() : this(File.Exists, ReadLastRunLocation, ReadRedMLastRunLocation)
    {
    }

    public ClientInstallLocator(Func<string, bool> exists, Func<string?> legacyInstallDirectoryProvider, Func<string?>? redmInstallDirectoryProvider = null)
    {
        _exists = exists;
        _legacyInstallDirectoryProvider = legacyInstallDirectoryProvider;
        _redmInstallDirectoryProvider = redmInstallDirectoryProvider ?? ReadRedMLastRunLocation;
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
            GameClient.FiveM => ResolveAppStyle(localAppData, _legacyInstallDirectoryProvider, "FiveM", ExecutableName),
            GameClient.FiveMEnhanced => ResolveEnhanced(localAppData),
            GameClient.RedM => ResolveAppStyle(localAppData, _redmInstallDirectoryProvider, "RedM", RedMExecutableName),
            _ => (null, null),
        };
    }

    private (string?, string?) ResolveAppStyle(string localAppData, Func<string?> provider, string folderName, string executableName)
    {
        var registered = provider();
        var installDirectory = string.IsNullOrWhiteSpace(registered)
            ? Path.Combine(localAppData, folderName, $"{folderName}.app")
            : NormalizeDirectory(registered);

        var insideApp = Path.Combine(installDirectory, executableName);
        var sibling = Path.Combine(Path.GetDirectoryName(installDirectory) ?? installDirectory, executableName);
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
        return ReadRegistryLastRunLocation(RegistryKeyPath);
    }

    private static string? ReadRedMLastRunLocation()
    {
        return ReadRegistryLastRunLocation(RedMRegistryKeyPath);
    }

    private static string? ReadRegistryLastRunLocation(string keyPath)
    {
        var value = Registry.GetValue(keyPath, RegistryValueName, null) as string;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}