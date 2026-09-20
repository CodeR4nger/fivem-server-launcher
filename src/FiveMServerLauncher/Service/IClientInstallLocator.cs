using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

public interface IClientInstallLocator
{
    Task<bool> IsInstalledAsync(GameClient client);
    Task<string?> GetExecutablePathAsync(GameClient client);
}