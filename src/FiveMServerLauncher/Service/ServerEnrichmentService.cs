using System.Net.Http;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public sealed record ServerPresence(
    bool Online,
    int Players,
    int MaxPlayers,
    GameClient? Game,
    string? IconVersion = null)
{
    public static ServerPresence Offline { get; } = new(false, 0, 0, null);
}

public interface IServerEnrichmentService
{
    Task<IReadOnlyDictionary<string, ServerPresence>?> RefreshAsync(bool forceRefresh = false);

    Task<byte[]?> GetIconAsync(string cfxId, string iconVersion);

    Task<string?> ResolveCfxIdAsync(string address);
}

public sealed class ServerEnrichmentService : IServerEnrichmentService
{
    private const string IconUrl = "https://frontend.cfx-services.net/api/servers/icon/{0}/{1}.png";

    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan DefaultForceCooldown = TimeSpan.FromSeconds(15);
    private const long DefaultMaxIconBytes = 2 * 1024 * 1024;
    private const int DefaultMaxConcurrentIconDownloads = 4;
    private const int DefaultMaxCachedIcons = 256;

    private readonly HttpClient _httpClient;
    private readonly ServerCatalog _catalog;
    private readonly IDnsResolver _dnsResolver;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _forceCooldown;
    private readonly long _maxIconBytes;
    private readonly int _maxCachedIcons;
    private readonly SemaphoreSlim _iconDownloadSlots;
    private readonly object _iconGate = new();
    private readonly LruCache<(string CfxId, string Version), byte[]> _iconCache;
    private readonly Dictionary<(string CfxId, string Version), Task<byte[]?>> _inFlightIcons = new();

    private DateTimeOffset _lastForcedAt = DateTimeOffset.MinValue;

    public ServerEnrichmentService(
        ServerCatalog catalog,
        HttpClient httpClient,
        IDnsResolver? dnsResolver = null,
        TimeProvider? timeProvider = null,
        TimeSpan? forceCooldown = null,
        long? maxIconBytes = null,
        int? maxConcurrentIconDownloads = null,
        int? maxCachedIcons = null)
    {
        _catalog = catalog;
        _httpClient = httpClient;
        _dnsResolver = dnsResolver ?? new DnsResolver();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _forceCooldown = forceCooldown ?? DefaultForceCooldown;
        _maxIconBytes = maxIconBytes ?? DefaultMaxIconBytes;
        _maxCachedIcons = Math.Max(1, maxCachedIcons ?? DefaultMaxCachedIcons);
        _iconDownloadSlots = new SemaphoreSlim(Math.Max(1, maxConcurrentIconDownloads ?? DefaultMaxConcurrentIconDownloads));
        _iconCache = new LruCache<(string CfxId, string Version), byte[]>(_maxCachedIcons);
    }

    public ServerEnrichmentService(HttpClient httpClient, TimeProvider? timeProvider = null, TimeSpan? cacheTtl = null, IDnsResolver? dnsResolver = null, TimeSpan? forceCooldown = null, long? maxIconBytes = null, int? maxConcurrentIconDownloads = null, int? maxCachedIcons = null)
        : this(new ServerCatalog(httpClient, timeProvider, cacheTtl ?? DefaultCacheTtl, dnsResolver), httpClient, dnsResolver, timeProvider, forceCooldown, maxIconBytes, maxConcurrentIconDownloads, maxCachedIcons)
    {
    }

    public async Task<IReadOnlyDictionary<string, ServerPresence>?> RefreshAsync(bool forceRefresh = false)
    {
        var mayForce = forceRefresh && _timeProvider.GetUtcNow() - _lastForcedAt >= _forceCooldown;
        var snapshot = await _catalog.GetSnapshotAsync(mayForce);

        if (snapshot is null)
        {
            return null;
        }

        if (mayForce)
        {
            _lastForcedAt = _timeProvider.GetUtcNow();
        }

        return snapshot
            .Where(s => s.Data is not null && !string.IsNullOrEmpty(s.EndPoint))
            .GroupBy(s => s.EndPoint!)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var server = g.First();

                    return new ServerPresence(
                        Online: true,
                        server.Data!.Clients,
                        server.Data.SvMaxclients,
                        CfxVars.TryGetGameClient(server.Data.Vars),
                        CfxVars.TryGetIconVersion(server.Data));
                });
    }

    public Task<byte[]?> GetIconAsync(string cfxId, string iconVersion)
    {
        var key = (cfxId, iconVersion);
        TaskCompletionSource<byte[]?>? pending = null;

        lock (_iconGate)
        {
            if (_iconCache.TryGet(key, out var cached))
            {
                return Task.FromResult<byte[]?>(cached);
            }

            // Single-flight per icon: concurrent requests (e.g. several browser rows for
            // the same server) share one download.
            if (_inFlightIcons.TryGetValue(key, out var inFlight))
            {
                return inFlight;
            }

            // Register before starting: a download that completes synchronously (the
            // routed test handler, OS-cached responses) must never leave a completed
            // task parked as in-flight to serve stale bytes after an eviction.
            pending = new TaskCompletionSource<byte[]?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _inFlightIcons[key] = pending.Task;
        }

        // Started outside the gate: a download that completes without suspending must
        // not hold the gate (and stall every other icon lookup) through its HTTP read.
        _ = CompleteIconDownloadAsync(key, pending);

        return pending.Task;
    }

    private async Task CompleteIconDownloadAsync(
        (string CfxId, string Version) key,
        TaskCompletionSource<byte[]?> completion)
    {
        try
        {
            completion.SetResult(await DownloadIconAsync(key));
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
        finally
        {
            lock (_iconGate)
            {
                _inFlightIcons.Remove(key);
            }
        }
    }

    private async Task<byte[]?> DownloadIconAsync((string CfxId, string Version) key)
    {
        try
        {
            await _iconDownloadSlots.WaitAsync();

            try
            {
                var response = await _httpClient.GetAsync(string.Format(IconUrl, key.CfxId, key.Version));

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var icon = await BoundedContent.ReadAsByteArrayAsync(response.Content, _maxIconBytes);

                if (icon is null)
                {
                    return null;
                }

                lock (_iconGate)
                {
                    _iconCache.Set(key, icon);
                }

                return icon;
            }
            finally
            {
                _iconDownloadSlots.Release();
            }
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    public async Task<string?> ResolveCfxIdAsync(string address)
    {
        var kind = ServerAddress.Classify(address);

        if (ServerAddress.IsIdForm(kind))
        {
            return ServerAddress.ExtractCfxId(address);
        }

        if (kind is ServerAddressKind.IpAddress or ServerAddressKind.DomainName)
        {
            return await ResolveBareAddressCfxIdAsync(address, kind);
        }

        var ipPort = kind switch
        {
            ServerAddressKind.IpPort => address,
            ServerAddressKind.DomainPort => await ResolveDomainToIpPortAsync(address),
            _ => null
        };

        if (ipPort is null)
        {
            return null;
        }

        var server = await _catalog.LookupByIpPortAsync(ipPort);

        return server?.EndPoint;
    }

    private async Task<string?> ResolveDomainToIpPortAsync(string address)
    {
        ServerAddress.TrySplitHostPort(address, out var host, out var port);

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port))
        {
            return null;
        }

        var ip = await _dnsResolver.ResolveToIpAsync(host);

        return ip is null ? null : $"{ip}:{port}";
    }

    private async Task<string?> ResolveBareAddressCfxIdAsync(string address, ServerAddressKind kind)
    {
        var server = kind == ServerAddressKind.DomainName
            ? await _catalog.LookupBareDomainAsync(address)
            : await _catalog.LookupBareIpAsync(address);

        return server?.EndPoint;
    }
}