using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

internal sealed class FakeCfxStatusService : ICfxStatusService
{
    public IReadOnlyDictionary<GameClient, CfxStatus>? Statuses { get; set; }

    public int GetCalls { get; private set; }

    public int ThrowOnGetCount { get; set; }

    public Task<IReadOnlyDictionary<GameClient, CfxStatus>?> GetStatusesAsync()
    {
        GetCalls++;
        if (ThrowOnGetCount > 0)
        {
            ThrowOnGetCount--;
            throw new InvalidOperationException("statuspage corrupt");
        }

        return Task.FromResult(Statuses);
    }
}