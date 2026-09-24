using System.Net.Http;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public class ServerCatalog(
    HttpClient httpClient,
    TimeProvider? timeProvider = null,
    TimeSpan? cacheTtl = null,
    IDnsResolver? dnsResolver = null)
{
    private static readonly string CatalogUrl = "https://frontend.cfx-services.net/api/servers/streamRedir/";

    private readonly HttpClient _httpClient = httpClient;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5);
    private readonly IDnsResolver _dnsResolver = dnsResolver ?? new DnsResolver();

    private IReadOnlyList<Master.Server>? _cachedServers;
    private DateTimeOffset _cachedAt;

    public async Task<Master.Server?> LookupByIpPortAsync(string ipPort)
    {
        var servers = await GetServersAsync();

        return servers.FirstOrDefault(
            s => s.Data is not null && s.Data.ConnectEndPoints.Contains(ipPort));
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
                .Select(NormalizeEndpointHost)
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

    internal static string NormalizeEndpointHost(string endpoint)
    {
        var withoutScheme = endpoint;
        var schemeIndex = withoutScheme.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex >= 0)
        {
            withoutScheme = withoutScheme[(schemeIndex + 3)..];
        }

        var pathIndex = withoutScheme.IndexOf('/');
        if (pathIndex >= 0)
        {
            withoutScheme = withoutScheme[..pathIndex];
        }

        var portIndex = withoutScheme.IndexOf(':');
        return portIndex >= 0 ? withoutScheme[..portIndex] : withoutScheme;
    }

    public async Task<IReadOnlyList<Master.Server>?> GetSnapshotAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && IsCacheValid())
        {
            return _cachedServers!;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, CatalogUrl);
            request.Headers.UserAgent.Add(
                new System.Net.Http.Headers.ProductInfoHeaderValue("FiveMServerLauncher", "0.1"));
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return forceRefresh ? _cachedServers : null;
            }

            var payload = await response.Content.ReadAsByteArrayAsync();
            var servers = await Task.Run(() => ServerCatalogDecoder.Decode(payload));

            CacheServers(servers);

            return servers;
        }
        catch (HttpRequestException)
        {
            return forceRefresh ? _cachedServers : null;
        }
        catch (TaskCanceledException)
        {
            return forceRefresh ? _cachedServers : null;
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
        _cachedServers = servers;
        _cachedAt = _timeProvider.GetUtcNow();
    }
}