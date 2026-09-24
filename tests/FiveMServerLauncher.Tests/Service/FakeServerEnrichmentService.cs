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

    public int DirectIconCalls { get; private set; }

    public string? LastRequestedIconVersion { get; private set; }

    public int ResolveCalls { get; private set; }

    public int ThrowOnRefreshCount { get; set; }

    public Task? RefreshDelay { get; set; }

    public Task<IReadOnlyDictionary<string, ServerPresence>?> RefreshAsync(bool forceRefresh = false)
    {
        return RefreshCoreAsync(forceRefresh);
    }

    private async Task<IReadOnlyDictionary<string, ServerPresence>?> RefreshCoreAsync(bool forceRefresh)
    {
        if (RefreshDelay is not null)
        {
            await RefreshDelay;
        }

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

        return Presence;
    }

    public Task<byte[]?> GetIconAsync(string cfxId)
    {
        IconCalls++;
        return Task.FromResult(Icon);
    }

    public Task<byte[]?> GetIconAsync(string cfxId, string iconVersion)
    {
        DirectIconCalls++;
        LastRequestedIconVersion = iconVersion;
        return Task.FromResult(Icon);
    }

    public Task<string?> ResolveCfxIdAsync(string address)
    {
        ResolveCalls++;
        return Task.FromResult(ResolvedCfxId);
    }
}