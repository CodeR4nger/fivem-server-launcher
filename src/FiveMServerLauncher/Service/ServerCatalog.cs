using System.Net.Http;

namespace FiveMServerLauncher.Service;

public class ServerCatalog(
    HttpClient httpClient,
    TimeProvider? timeProvider = null,
    TimeSpan? cacheTtl = null)
{
    private static readonly string CatalogUrl = "https://frontend.cfx-services.net/api/servers/streamRedir/";

    private readonly HttpClient _httpClient = httpClient;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly TimeSpan _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(5);

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

    public async Task<IReadOnlyList<Master.Server>?> GetSnapshotAsync()
    {
        if (IsCacheValid())
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
                return null;
            }

            var payload = await response.Content.ReadAsByteArrayAsync();
            var servers = ServerCatalogDecoder.Decode(payload);

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
        _cachedServers = servers;
        _cachedAt = _timeProvider.GetUtcNow();
    }
}