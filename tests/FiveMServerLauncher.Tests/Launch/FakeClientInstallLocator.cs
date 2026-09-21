using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;
using System.IO;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeClientInstallLocator : IClientInstallLocator
{
    public string? InstallDirectory { get; set; } = @"C:\FiveM\FiveM.app";
    public bool Throw { get; set; }

    public Task<bool> IsInstalledAsync(GameClient client)
    {
        return Task.FromResult(InstallDirectory is not null);
    }

    public Task<string?> GetExecutablePathAsync(GameClient client)
    {
        return Task.FromResult<string?>(InstallDirectory is null ? null : @"C:\FiveM\FiveM.app\FiveM.exe");
    }

    public Task<string?> GetInstallDirectoryAsync(GameClient client)
    {
        if (Throw)
        {
            throw new IOException("Simulated locator failure");
        }

        return Task.FromResult(InstallDirectory);
    }
}