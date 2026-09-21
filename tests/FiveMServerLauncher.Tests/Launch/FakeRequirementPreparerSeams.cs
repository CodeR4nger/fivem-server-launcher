using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class StatefulRequirementReadiness(Func<ExternalApp, bool> running) : IRequirementReadiness
{
    public StatefulRequirementReadiness() : this(_ => true)
    {
    }

    public int Checks { get; private set; }

    public Task<bool> IsRunningAsync(ExternalApp app)
    {
        Checks++;
        return Task.FromResult(running(app));
    }
}

internal sealed class FakeExternalAppStarter : IExternalAppStarter
{
    public List<ExternalApp> Starts { get; } = [];

    public Exception? ThrowOnStart { get; set; }

    public Task StartAsync(ExternalApp app)
    {
        if (ThrowOnStart is not null)
        {
            throw ThrowOnStart;
        }

        Starts.Add(app);
        return Task.CompletedTask;
    }
}