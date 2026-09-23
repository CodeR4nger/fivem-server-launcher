using System.Net;
using System.Net.Http;
using System.Text.Json;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public sealed record ServerPresence(bool Online, int Players, int MaxPlayers, GameClient? Game);

public interface IServerEnrichmentService
{
    Task<IReadOnlyDictionary<string, ServerPresence>?> RefreshAsync(bool forceRefresh = false);

    Task<byte[]?> GetIconAsync(string cfxId);

    Task<string?> ResolveCfxIdAsync(string address);
}

public sealed class ServerEnrichmentService : IServerEnrichmentService
{
    private const string SingleUrl = "https://frontend.cfx-services.net/api/servers/single/{0}";
    private const string IconUrl = "https://frontend.cfx-services.net/api/servers/icon/{0}/{1}.png";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan DefaultForceCooldown = TimeSpan.FromSeconds(15);

    private readonly HttpClient _httpClient;
    private readonly ServerCatalog _catalog;
    private readonly IDnsResolver _dnsResolver;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _forceCooldown;
    private readonly Dictionary<(string CfxId, string Version), byte[]> _iconCache = new();
    private DateTimeOffset _lastForcedAt = DateTimeOffset.MinValue;

    public ServerEnrichmentService(
        ServerCatalog catalog,
        HttpClient httpClient,
        IDnsResolver? dnsResolver = null,
        TimeProvider? timeProvider = null,
        TimeSpan? forceCooldown = null)
    {
        _catalog = catalog;
        _httpClient = httpClient;
        _dnsResolver = dnsResolver ?? new DnsResolver();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _forceCooldown = forceCooldown ?? DefaultForceCooldown;
    }

    public ServerEnrichmentService(HttpClient httpClient, TimeProvider? timeProvider = null, TimeSpan? cacheTtl = null, IDnsResolver? dnsResolver = null, TimeSpan? forceCooldown = null)
        : this(new ServerCatalog(httpClient, timeProvider, cacheTtl ?? DefaultCacheTtl, dnsResolver), httpClient, dnsResolver, timeProvider, forceCooldown)
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
                g => new ServerPresence(Online: true, g.First().Data!.Clients, g.First().Data.SvMaxclients, CfxVars.TryGetGameClient(g.First().Data.Vars)));
    }

    public async Task<byte[]?> GetIconAsync(string cfxId)
    {
        var version = await GetIconVersionAsync(cfxId);

        if (version is null)
        {
            return null;
        }

        var key = (cfxId, version);

        if (_iconCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        try
        {
            var icon = await _httpClient.GetByteArrayAsync(string.Format(IconUrl, cfxId, version));
            _iconCache[key] = icon;
            return icon;
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

        if (kind is ServerAddressKind.CfxId or ServerAddressKind.CfxJoinUrl)
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

    private async Task<string?> GetIconVersionAsync(string cfxId)
    {
        try
        {
            var response = await _httpClient.GetAsync(string.Format(SingleUrl, cfxId));

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonSerializer.Deserialize<CfxServerResponse>(json, SerializerOptions);

            return parsed?.Data?.IconVersion?.ToString();
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class CfxServerResponse
    {
        public CfxServerData? Data { get; set; }
    }

    private sealed class CfxServerData
    {
        public long? IconVersion { get; set; }
    }
}