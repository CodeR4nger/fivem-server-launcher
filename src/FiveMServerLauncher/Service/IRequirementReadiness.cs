using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

public interface IRequirementReadiness
{
    Task<bool> IsRunningAsync(ExternalApp app);

    Task<bool> IsReadyAsync(ExternalApp app);
}