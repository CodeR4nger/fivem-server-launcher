using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Launch;

public sealed class ExternalAppPreparer(
    IRequirementReadiness readiness,
    IExternalAppStarter starter,
    Func<Task>? wait = null,
    int maxAttempts = 120)
{
    private const int PollIntervalMilliseconds = 500;

    private readonly Func<Task> _wait = wait ?? (() => Task.Delay(PollIntervalMilliseconds));

    public async Task<bool> TryPrepareAsync(ExternalApp app)
    {
        try
        {
            if (await readiness.IsRunningAsync(app))
            {
                return true;
            }

            await starter.StartAsync(app);

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (await readiness.IsRunningAsync(app))
                {
                    return true;
                }

                await _wait();
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}