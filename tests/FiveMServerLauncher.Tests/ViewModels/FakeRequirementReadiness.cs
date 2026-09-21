using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.ViewModels;

public class FakeRequirementReadiness : IRequirementReadiness
{
    public bool Running { get; set; } = true;

    public bool? Ready { get; set; }

    public int SteamChecks { get; private set; }

    public int DiscordChecks { get; private set; }

    public int SteamReadyChecks { get; private set; }

    public Task<bool> IsRunningAsync(ExternalApp app)
    {
        if (app == ExternalApp.Steam)
        {
            SteamChecks++;
        }

        if (app == ExternalApp.Discord)
        {
            DiscordChecks++;
        }

        return Task.FromResult(Running);
    }

    public Task<bool> IsReadyAsync(ExternalApp app)
    {
        if (app == ExternalApp.Steam)
        {
            SteamReadyChecks++;
        }

        return Task.FromResult(Ready ?? Running);
    }
}