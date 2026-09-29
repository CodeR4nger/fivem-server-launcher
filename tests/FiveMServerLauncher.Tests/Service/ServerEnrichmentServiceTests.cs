using System.Net;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Domain;
using Xunit;

namespace FiveMServerLauncher.Tests.Service;

public class ServerEnrichmentServiceTests
{
    [Fact]
    public async Task RefreshAsync_WhenServerInCatalog_ShouldReportOnlineWithPlayersAndMax()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { Clients = 12, SvMaxclients = 64 }
            }));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        var presence = Assert.Single(result!);
        Assert.Equal("y4lg95", presence.Key);
        Assert.True(presence.Value.Online);
        Assert.Equal(12, presence.Value.Players);
        Assert.Equal(64, presence.Value.MaxPlayers);
    }

    [Fact]
    public async Task RefreshAsync_WhenGamenamePublished_ShouldExposeGameClient()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        var frame = new Master.Server
        {
            EndPoint = "boya5d",
            Data = new Master.ServerData
            {
                Clients = 2,
                SvMaxclients = 48,
                Vars = { ["gamename"] = "rdr3" }
            }
        };
        handler.AddBytesRoute("streamRedir", HttpStatusCode.OK, TestProtobufFrames.BuildFrameStream(frame));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        var presence = Assert.Single(result!);
        Assert.Equal(GameClient.RedM, presence.Value.Game);
    }

    [Fact]
    public async Task RefreshAsync_WhenUnknownGamename_ShouldExposeNullGame()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        var frame = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData { Clients = 1, SvMaxclients = 32 }
        };
        handler.AddBytesRoute("streamRedir", HttpStatusCode.OK, TestProtobufFrames.BuildFrameStream(frame));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        var presence = Assert.Single(result!);
        Assert.Null(presence.Value.Game);
    }

    [Fact]
    public async Task RefreshAsync_WhenServerAbsent_ShouldNotIncludeIt()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "other",
                Data = new Master.ServerData { Clients = 1, SvMaxclients = 32 }
            }));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        Assert.NotNull(result);
        Assert.False(result.ContainsKey("y4lg95"));
    }

    [Fact]
    public async Task RefreshAsync_WhenCatalogUnavailable_ShouldReturnNullWithoutThrowing()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("streamRedir", HttpStatusCode.InternalServerError, Array.Empty<byte>());
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshAsync_WhenNetworkFails_ShouldReturnNullWithoutThrowing()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("streamRedir", throwOnSend: true);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshAsync_WhenDuplicateEndPointInFrames_ShouldNotThrow()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.Join(
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "y4lg95",
                    Data = new Master.ServerData { Clients = 12, SvMaxclients = 64 }
                }),
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "y4lg95",
                    Data = new Master.ServerData { Clients = 5, SvMaxclients = 32 }
                })));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        var presence = Assert.Single(result!);
        Assert.True(presence.Value.Online);
    }

    [Fact]
    public async Task RefreshAsync_WhenIconVersionVarPresentOrAbsent_ShouldExposeItOnPresence()
    {
        // Given — the catalog snapshot already carries each server's iconVersion in the
        // vars, so the enrichment loop can use the direct icon path without probing
        // /single/ per row.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.Join(
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "withicon",
                    Data = new Master.ServerData { Vars = { ["iconVersion"] = "288154985" } }
                }),
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "noicon",
                    Data = new Master.ServerData { Clients = 1, SvMaxclients = 32 }
                })));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.RefreshAsync();

        // Then
        Assert.Equal("288154985", result!["withicon"].IconVersion);
        Assert.Null(result["noicon"].IconVersion);
    }

    [Fact]
    public async Task GetIconAsync_WhenIconExceedsMaxSize_ShouldReturnNull()
    {
        // Given — a decompression-bomb style icon is dropped, never decoded into the cache.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/y4lg95/7.png", HttpStatusCode.OK, new byte[500]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, maxIconBytes: 100);

        // When
        var result = await service.GetIconAsync("y4lg95", "7");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIconAsync_WhenVersionUnchanged_ShouldServeCachedIconWithoutRedownload()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/y4lg95/288154985.png", HttpStatusCode.OK, [1, 2, 3, 4]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var first = await service.GetIconAsync("y4lg95", "288154985");
        var second = await service.GetIconAsync("y4lg95", "288154985");

        // Then
        Assert.Equal([1, 2, 3, 4], first);
        Assert.Equal([1, 2, 3, 4], second);
        Assert.Equal(1, handler.RequestCountByPath("icon/y4lg95/288154985.png"));
    }

    [Fact]
    public async Task GetIconAsync_WhenVersionChanges_ShouldDownloadNewIcon()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/y4lg95/1.png", HttpStatusCode.OK, [1]);
        handler.AddBytesRoute("icon/y4lg95/2.png", HttpStatusCode.OK, [2]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var first = await service.GetIconAsync("y4lg95", "1");
        var second = await service.GetIconAsync("y4lg95", "2");

        // Then
        Assert.Equal([1], first);
        Assert.Equal([2], second);
        Assert.Equal(1, handler.RequestCountByPath("icon/y4lg95/1.png"));
        Assert.Equal(1, handler.RequestCountByPath("icon/y4lg95/2.png"));
    }

    [Fact]
    public async Task GetIconAsync_WhenIconDownloadFails_ShouldReturnNullWithoutThrowing()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/y4lg95/5.png", HttpStatusCode.NotFound, Array.Empty<byte>());
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.GetIconAsync("y4lg95", "5");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIconAsync_WhenDownloadFaultsWithUnexpectedException_ShouldPropagateToJoinedCallers()
    {
        // Given — only outages (HTTP/timeout) degrade to null; an unexpected fault
        // propagates to every caller sharing the in-flight download.
        var handler = new FaultingIconHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When — caller two joins the in-flight download before it faults.
        var first = service.GetIconAsync("y4lg95", "7");
        var second = service.GetIconAsync("y4lg95", "7");
        Assert.Equal(1, handler.TotalRequests);
        handler.Release();

        // Then
        await Assert.ThrowsAsync<InvalidOperationException>(() => first);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        Assert.Equal(1, handler.TotalRequests);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithIpPortInCatalog_ShouldReturnEndPoint()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30320" } }
            }));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.ResolveCfxIdAsync("149.56.120.52:30320");

        // Then
        Assert.Equal("y4lg95", result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithIpPortNotInCatalog_ShouldReturnNull()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30320" } }
            }));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.ResolveCfxIdAsync("10.0.0.1:9999");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithCfxJoinUrl_ShouldExtractIdWithoutCatalog()
    {
        // Given
        using var httpClient = new HttpClient(new HttpMessageHandlerStub());
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.ResolveCfxIdAsync("cfx.re/join/y4lg95");

        // Then
        Assert.Equal("y4lg95", result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithDomainPortResolvedToCatalogIp_ShouldReturnEndPoint()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30320" } }
            }));
        using var httpClient = new HttpClient(handler);
        var dns = new FakeDnsResolver("149.56.120.52");
        var service = new ServerEnrichmentService(httpClient, dnsResolver: dns);

        // When
        var result = await service.ResolveCfxIdAsync("play.example.com:30320");

        // Then
        Assert.Equal("y4lg95", result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WhenDomainPortDnsFails_ShouldReturnNull()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var dns = new FakeDnsResolver();
        var service = new ServerEnrichmentService(httpClient, dnsResolver: dns);

        // When
        var result = await service.ResolveCfxIdAsync("play.example.com:30320");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithLocalhostResolvedToLoopbackCatalogEndpoint_ShouldReturnNullBecauseEndpointIsHidden()
    {
        // Given — the catalog entry behind the loopback IP is a hidden server; saving a
        // localhost address must never capture a stranger's id.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "8y6354",
                Data = new Master.ServerData { ConnectEndPoints = { "127.0.0.1:30120" } }
            }));
        using var httpClient = new HttpClient(handler);
        var dns = new FakeDnsResolver("127.0.0.1");
        var service = new ServerEnrichmentService(httpClient, dnsResolver: dns);

        // When
        var result = await service.ResolveCfxIdAsync("localhost:30120");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WhenLocalhostUnlisted_ShouldReturnNullWithoutThrowing()
    {
        // Given — an unlisted local dev server stays plain; the capture degrades silently.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "other" }));
        using var httpClient = new HttpClient(handler);
        var dns = new FakeDnsResolver("127.0.0.1");
        var service = new ServerEnrichmentService(httpClient, dnsResolver: dns);

        // When
        var result = await service.ResolveCfxIdAsync("localhost:30120");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithBareLocalhostResolvedToLoopbackCatalogEndpoint_ShouldReturnNullBecauseEndpointIsHidden()
    {
        // Given — the port-less localhost form (v1.2) must not capture a hidden id either.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "8y6354",
                Data = new Master.ServerData { ConnectEndPoints = { "127.0.0.1:30120" } }
            }));
        using var httpClient = new HttpClient(handler);
        var dns = new FakeDnsResolver("127.0.0.1");
        var service = new ServerEnrichmentService(httpClient, dnsResolver: dns);

        // When
        var result = await service.ResolveCfxIdAsync("localhost");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithBareIpInCatalogOnDefaultPort_ShouldReturnEndPoint()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30120" } }
            }));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.ResolveCfxIdAsync("149.56.120.52");

        // Then
        Assert.Equal("y4lg95", result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithBareDomainMatchingProxyEndpointHost_ShouldReturnEndPoint()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { ConnectEndPoints = { "https://play.example.com:443/" } }
            }));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.ResolveCfxIdAsync("play.example.com");

        // Then
        Assert.Equal("y4lg95", result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithBareDomainHostAmbiguous_ShouldReturnNull()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.Join(
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "aaaaaa",
                    Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io/" } }
                }),
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "bbbbbb",
                    Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io:443/" } }
                })));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, dnsResolver: new FakeDnsResolver());

        // When
        var result = await service.ResolveCfxIdAsync("proxy.shared.io");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithBareDomainResolvedToCatalogIpDefaultPort_ShouldReturnEndPoint()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30120" } }
            }));
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(
            httpClient, dnsResolver: new FakeDnsResolver("149.56.120.52"));

        // When
        var result = await service.ResolveCfxIdAsync("direct.example.com");

        // Then
        Assert.Equal("y4lg95", result);
    }

    [Fact]
    public async Task ResolveCfxIdAsync_WithBareIpNotInCatalog_ShouldReturnNull()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.ResolveCfxIdAsync("10.0.0.1");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIconAsync_WithKnownVersion_ShouldDownloadDirectlyWithoutSingleCall()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/y4lg95/55.png", HttpStatusCode.OK, [1, 2, 3]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var icon = await service.GetIconAsync("y4lg95", "55");

        // Then
        Assert.Equal(new byte[] { 1, 2, 3 }, icon);
        Assert.Equal(0, handler.RequestCountByPath("single"));
    }

    [Fact]
    public async Task GetIconAsync_WithKnownVersion_ShouldCacheByIdAndVersion()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/y4lg95/55.png", HttpStatusCode.OK, [1, 2, 3]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        await service.GetIconAsync("y4lg95", "55");
        var again = await service.GetIconAsync("y4lg95", "55");

        // Then
        Assert.Equal(new byte[] { 1, 2, 3 }, again);
        Assert.Equal(1, handler.RequestCountByPath("icon/y4lg95/55.png"));
    }

    [Fact]
    public async Task GetIconAsync_WhenConcurrentRequestsForSameIcon_ShouldShareOneDownload()
    {
        // Given — browser scrolling can realize several rows for the same server at
        // once; concurrent requests for one icon must share a single download.
        var handler = new GatedIconHttpMessageHandler([1, 2, 3]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var requests = Enumerable.Range(0, 6)
            .Select(_ => service.GetIconAsync("y4lg95", "7"))
            .ToList();
        Assert.Equal(1, handler.TotalRequests);
        handler.Release();
        var icons = await Task.WhenAll(requests);

        // Then
        Assert.All(icons, icon => Assert.Equal(new byte[] { 1, 2, 3 }, icon));
        Assert.Equal(1, handler.TotalRequests);
    }

    [Fact]
    public async Task GetIconAsync_WhenManyDistinctIconsRequestedConcurrently_ShouldBoundInFlightDownloads()
    {
        // Given — scrolling a huge catalog must not open an unbounded number of
        // parallel icon downloads; concurrent downloads are capped.
        var handler = new GatedIconHttpMessageHandler([1, 2, 3]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, maxConcurrentIconDownloads: 2);

        // When
        var requests = Enumerable.Range(0, 6)
            .Select(i => service.GetIconAsync($"srv{i}", "7"))
            .ToList();
        Assert.Equal(2, handler.TotalRequests);
        handler.Release();
        var icons = await Task.WhenAll(requests);

        // Then — exactly two downloads were ever in flight: bounded, but not serialized.
        Assert.All(icons, icon => Assert.Equal(new byte[] { 1, 2, 3 }, icon));
        Assert.Equal(6, handler.TotalRequests);
        Assert.Equal(2, handler.PeakInFlight);
    }

    [Fact]
    public async Task GetIconAsync_WhenCacheExceedsCapacity_ShouldEvictLeastRecentlyUsedIcon()
    {
        // Given — the icon cache is bounded for the app's lifetime; beyond capacity the
        // least recently used icon is evicted (a re-touched older icon survives).
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/srv0/1.png", HttpStatusCode.OK, [10]);
        handler.AddBytesRoute("icon/srv1/1.png", HttpStatusCode.OK, [11]);
        handler.AddBytesRoute("icon/srv2/1.png", HttpStatusCode.OK, [12]);
        handler.AddBytesRoute("icon/srv3/1.png", HttpStatusCode.OK, [13]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, maxCachedIcons: 3);

        // When
        await service.GetIconAsync("srv0", "1");
        await service.GetIconAsync("srv1", "1");
        await service.GetIconAsync("srv2", "1");
        await service.GetIconAsync("srv0", "1");
        await service.GetIconAsync("srv3", "1");
        var evicted = await service.GetIconAsync("srv1", "1");
        var retained = await service.GetIconAsync("srv0", "1");

        // Then — srv1 was evicted (LRU: srv0 was re-touched and survived); srv0 stays cached.
        Assert.Equal(new byte[] { 11 }, evicted);
        Assert.Equal(new byte[] { 10 }, retained);
        Assert.Equal(2, handler.RequestCountByPath("icon/srv1/1.png"));
        Assert.Equal(1, handler.RequestCountByPath("icon/srv0/1.png"));
        Assert.Equal(1, handler.RequestCountByPath("icon/srv3/1.png"));
    }

    [Fact]
    public async Task GetIconAsync_WhenDownloadCompletesSynchronously_ShouldNotParkCompletedTaskAsInFlight()
    {
        // Given — a download can finish before GetIconAsync even registers it (the
        // routed handler answers synchronously); a completed task parked as in-flight
        // would later serve stale bytes with no download after an eviction.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("icon/srva/1.png", HttpStatusCode.OK, [10]);
        handler.AddBytesRoute("icon/srvb/1.png", HttpStatusCode.OK, [11]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, maxCachedIcons: 1);

        // When
        await service.GetIconAsync("srva", "1");
        await service.GetIconAsync("srvb", "1");
        var refetched = await service.GetIconAsync("srva", "1");

        // Then — srva was evicted by srvb, so the re-request downloads again.
        Assert.Equal(new byte[] { 10 }, refetched);
        Assert.Equal(2, handler.RequestCountByPath("icon/srva/1.png"));
    }

    private sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FaultingIconHttpMessageHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _gate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requests;

        public int TotalRequests => Volatile.Read(ref _requests);

        public void Release()
        {
            _gate.TrySetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            await _gate.Task;
            throw new InvalidOperationException("Unexpected handler fault");
        }
    }

    private sealed class GatedIconHttpMessageHandler(byte[] payload) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _gate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _peakGate = new();
        private int _inFlight;

        public int TotalRequests { get; private set; }

        public int PeakInFlight { get; private set; }

        public void Release()
        {
            _gate.TrySetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            lock (_peakGate)
            {
                TotalRequests++;
                _inFlight++;
                PeakInFlight = Math.Max(PeakInFlight, _inFlight);
            }

            try
            {
                await _gate.Task;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(payload)
                };
            }
            finally
            {
                lock (_peakGate)
                {
                    _inFlight--;
                }
            }
        }
    }

    [Fact]
    public async Task RefreshAsync_WhenForced_ShouldBypassCatalogTtlAndFetch()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { Clients = 3, SvMaxclients = 48 }
            }));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var service = new ServerEnrichmentService(httpClient, clock, TimeSpan.FromMinutes(5));

        // When
        await service.RefreshAsync();
        await service.RefreshAsync(forceRefresh: true);

        // Then
        Assert.Equal(2, handler.RequestCountByPath("streamRedir"));
    }

    [Fact]
    public async Task RefreshAsync_WhenForcedInsideCooldown_ShouldServeCache()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { Clients = 3, SvMaxclients = 48 }
            }));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var service = new ServerEnrichmentService(httpClient, clock, TimeSpan.FromMinutes(5));

        // When
        await service.RefreshAsync(forceRefresh: true);
        await service.RefreshAsync(forceRefresh: true);

        // Then
        Assert.Equal(1, handler.RequestCountByPath("streamRedir"));
    }

    [Fact]
    public async Task RefreshAsync_WhenForcedAfterCooldown_ShouldRefetch()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(new Master.Server
            {
                EndPoint = "y4lg95",
                Data = new Master.ServerData { Clients = 3, SvMaxclients = 48 }
            }));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var service = new ServerEnrichmentService(
            httpClient, clock, TimeSpan.FromMinutes(5), forceCooldown: TimeSpan.FromSeconds(15));

        // When
        await service.RefreshAsync(forceRefresh: true);
        clock.Advance(TimeSpan.FromSeconds(16));
        await service.RefreshAsync(forceRefresh: true);

        // Then
        Assert.Equal(2, handler.RequestCountByPath("streamRedir"));
    }
}

