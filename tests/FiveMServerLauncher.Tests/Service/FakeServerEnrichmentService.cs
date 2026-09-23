using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

internal sealed class FakeServerEnrichmentService : IServerEnrichmentService
{
    public IReadOnlyDictionary<string, ServerPresence>? Presence { get; set; }

    public byte[]? Icon { get; set; }

    public string? ResolvedCfxId { get; set; }

    public int RefreshCalls { get; private set; }

    public int ForcedRefreshCalls { get; private set; }

    public int IconCalls { get; private set; }

    public int ResolveCalls { get; private set; }

    public int ThrowOnRefreshCount { get; set; }

    public Task<IReadOnlyDictionary<string, ServerPresence>?> RefreshAsync(bool forceRefresh = false)
    {
        RefreshCalls++;
        if (forceRefresh)
        {
            ForcedRefreshCalls++;
        }

        if (ThrowOnRefreshCount > 0)
        {
            ThrowOnRefreshCount--;
            throw new InvalidOperationException("catalog corrupt");
        }

        return Task.FromResult(Presence);
    }

    public Task<byte[]?> GetIconAsync(string cfxId)
    {
        IconCalls++;
        return Task.FromResult(Icon);
    }

    public Task<string?> ResolveCfxIdAsync(string address)
    {
        ResolveCalls++;
        return Task.FromResult(ResolvedCfxId);
    }
}