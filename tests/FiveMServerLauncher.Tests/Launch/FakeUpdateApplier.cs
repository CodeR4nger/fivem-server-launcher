using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeUpdateApplier : IUpdateApplier
{
    public LauncherUpdate? Received { get; private set; }

    public bool ApplyResult { get; set; } = true;

    public int ApplyCalls { get; private set; }

    public int ThrowOnApplyCount { get; set; }

    public TaskCompletionSource<bool>? PendingApply { get; set; }

    public Task<bool> ApplyAsync(LauncherUpdate update)
    {
        ApplyCalls++;
        Received = update;

        if (ThrowOnApplyCount > 0)
        {
            ThrowOnApplyCount--;
            throw new InvalidOperationException("apply failed");
        }

        return PendingApply?.Task ?? Task.FromResult(ApplyResult);
    }
}
