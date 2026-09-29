using System.Net.Http;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public class ServerCatalog(
    HttpClient httpClient,
    TimeProvider? timeProvider = null,
    TimeSpan? cacheTtl = null,
    IDnsResolver? dnsResolver = null,
    long? maxPayloadBytes = null)
{
    private static readonly string CatalogUrl = "https://frontend.cfx-services.net/api/servers/streamRedir/";
    private const long DefaultMaxPayloadBytes = 64 * 1024 * 1024;

    private readonly HttpClient _httpClient = httpClient;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5);
    private readonly IDnsResolver _dnsResolver = dnsResolver ?? new DnsResolver();
    private readonly long _maxPayloadBytes = maxPayloadBytes ?? DefaultMaxPayloadBytes;

    private IReadOnlyList<Master.Server>? _cachedServers;
    private DateTimeOffset _cachedAt;

    private readonly object _snapshotGate = new();
    private Task<IReadOnlyList<Master.Server>?>? _snapshotFetch;

    public async Task<Master.Server?> LookupByIpPortAsync(string ipPort)
    {
        var servers = await GetServersAsync();

        return servers.FirstOrDefault(
            s => s.Data is not null && s.Data.ConnectEndPoints
                .Where(e => !CatalogEndpoint.IsHidden(e))
                .Contains(ipPort));
    }

    public async Task<Master.Server?> LookupByEndPointAsync(string endPoint)
    {
        var servers = await GetServersAsync();

        return servers.FirstOrDefault(s => s.EndPoint == endPoint);
    }

    public async Task<IReadOnlyList<Master.Server>> FindByEndpointHostAsync(string host)
    {
        var servers = await GetServersAsync();

        return servers
            .Where(s => s.Data is not null && s.Data.ConnectEndPoints
                .Where(e => !CatalogEndpoint.IsHidden(e))
                .Select(CatalogEndpoint.HostOf)
                .Any(h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<Master.Server?> LookupBareIpAsync(string ip)
    {
        return await LookupByIpPortAsync($"{ip}:{ServerAddress.DefaultPort}");
    }

    public async Task<Master.Server?> LookupBareDomainAsync(string domain)
    {
        var byHost = await FindByEndpointHostAsync(domain);

        // An ambiguous shared proxy host must never guess one of its servers.
        if (byHost.Count > 1)
        {
            return null;
        }

        if (byHost.Count == 1)
        {
            return byHost[0];
        }

        var ip = await _dnsResolver.ResolveToIpAsync(domain);

        return ip is null ? null : await LookupByIpPortAsync($"{ip}:{ServerAddress.DefaultPort}");
    }

    public Task<IReadOnlyList<Master.Server>?> GetSnapshotAsync(bool forceRefresh = false)
    {
        Task<IReadOnlyList<Master.Server>?> fetch;

        // The gate owns both cache fields and the in-flight task, so the cache write
        // is coherent with every read.
        lock (_snapshotGate)
        {
            if (!forceRefresh && IsCacheValid())
            {
                return Task.FromResult<IReadOnlyList<Master.Server>?>(_cachedServers!);
            }

            // Single-flight: concurrent callers (connect, enrichment loop, browser)
            // share one in-flight download; only that task writes the cache. A forced
            // refresh joins an in-flight download too — it is fresher than any cache.
            if (_snapshotFetch is null || _snapshotFetch.IsCompleted)
            {
                _snapshotFetch = DownloadSnapshotAsync();
            }

            fetch = _snapshotFetch;
        }

        return forceRefresh ? ServeWarmCacheWhenOutageAsync(fetch) : fetch;
    }

    private async Task<IReadOnlyList<Master.Server>?> ServeWarmCacheWhenOutageAsync(
        Task<IReadOnlyList<Master.Server>?> fetch)
    {
        return await fetch ?? _cachedServers;
    }

    private async Task<IReadOnlyList<Master.Server>?> DownloadSnapshotAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, CatalogUrl);
            request.Headers.UserAgent.Add(
                new System.Net.Http.Headers.ProductInfoHeaderValue("FiveMServerLauncher", "0.1"));
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await BoundedContent.ReadAsByteArrayAsync(response.Content, _maxPayloadBytes);

            if (payload is null)
            {
                return null;
            }

            var servers = await Task.Run(() => ServerCatalogDecoder.Decode(payload));

            CacheServers(servers);

            return servers;
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

    private async Task<IReadOnlyList<Master.Server>> GetServersAsync()
    {
        return await GetSnapshotAsync() ?? [];
    }

    private bool IsCacheValid()
    {
        return _cachedServers is not null &&
               _timeProvider.GetUtcNow() - _cachedAt < _cacheTtl;
    }

    private void CacheServers(IReadOnlyList<Master.Server> servers)
    {
        lock (_snapshotGate)
        {
            _cachedServers = servers;
            _cachedAt = _timeProvider.GetUtcNow();
        }
    }
}