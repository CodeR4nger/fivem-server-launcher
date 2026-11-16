using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

internal sealed class FakeReleaseFeed : IReleaseFeed
{
    public LauncherUpdate? Update { get; set; }

    public int CheckCalls { get; private set; }

    public int ThrowOnCheckCount { get; set; }

    // Holds the check open until the test completes it (in-flight pins).
    public TaskCompletionSource<LauncherUpdate?>? PendingCheck { get; set; }

    public Task<LauncherUpdate?> CheckForUpdateAsync()
    {
        CheckCalls++;

        if (ThrowOnCheckCount > 0)
        {
            ThrowOnCheckCount--;
            throw new InvalidOperationException("feed corrupt");
        }

        return PendingCheck?.Task ?? Task.FromResult(Update);
    }
}
