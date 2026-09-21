using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class StatefulRequirementReadiness(
    Func<ExternalApp, bool>? running = null,
    Func<ExternalApp, bool>? ready = null) : IRequirementReadiness
{
    public StatefulRequirementReadiness() : this(null, null)
    {
    }

    public int RunningChecks { get; private set; }

    public int ReadyChecks { get; private set; }

    public Task<bool> IsRunningAsync(ExternalApp app)
    {
        RunningChecks++;
        return Task.FromResult(running?.Invoke(app) ?? true);
    }

    public Task<bool> IsReadyAsync(ExternalApp app)
    {
        ReadyChecks++;
        return Task.FromResult(ready?.Invoke(app) ?? running?.Invoke(app) ?? true);
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