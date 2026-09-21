using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;
using System.IO;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeClientInstallLocator : IClientInstallLocator
{
    public Dictionary<GameClient, string> Executables { get; set; } = new()
    {
        [GameClient.FiveM] = @"C:\FiveM\FiveM.app\FiveM.exe",
    };

    public bool Throw { get; set; }

    public Task<bool> IsInstalledAsync(GameClient client)
    {
        return Task.FromResult(Executables.ContainsKey(client));
    }

    public Task<string?> GetExecutablePathAsync(GameClient client)
    {
        return Task.FromResult<string?>(Executables.GetValueOrDefault(client));
    }

    public Task<string?> GetInstallDirectoryAsync(GameClient client)
    {
        if (Throw)
        {
            throw new IOException("Simulated locator failure");
        }

        return Task.FromResult<string?>(
            Executables.TryGetValue(client, out var executable) ? Path.GetDirectoryName(executable) : null);
    }
}