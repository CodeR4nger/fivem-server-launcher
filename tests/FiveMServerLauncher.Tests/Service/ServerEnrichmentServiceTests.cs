using System.IO;
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
    public async Task RefreshAsync_WhenIconUrlPublishedInVars_ShouldExposeItOnPresence()
    {
        // Given — CFX no longer publishes the typed iconVersion field; the icon
        // truth is the `icon_url` var carried in the catalog snapshot (verified live
        // 2026-10-26: 0/33,548 servers publish the field, 25,697 publish the var).
        // A missing or empty var means the server has no icon.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "streamRedir",
            HttpStatusCode.OK,
            TestProtobufFrames.Join(
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "withicon",
                    Data = new Master.ServerData
                    {
                        Clients = 1,
                        SvMaxclients = 32,
                        Vars = { ["icon_url"] = "https://cfx-server-metadata.cfx-services.net/images/7665c31f-234f-4758-bc1a-a24d2eb4f530" }
                    }
                }),
                TestProtobufFrames.BuildFrameStream(new Master.Server
                {
                    EndPoint = "emptyvar",
                    Data = new Master.ServerData { Clients = 1, SvMaxclients = 32, Vars = { ["icon_url"] = "" } }
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
        Assert.Equal(
            "https://cfx-server-metadata.cfx-services.net/images/7665c31f-234f-4758-bc1a-a24d2eb4f530",
            result!["withicon"].IconUrl);
        Assert.Null(result["emptyvar"].IconUrl);
        Assert.Null(result["noicon"].IconUrl);
    }

    [Fact]
    public async Task GetIconAsync_WhenIconExceedsLegacySizeCap_ShouldServeItUnderTheRaisedDefaultCap()
    {
        // Given — real CFX metadata icons (observed live 2026-10-26) run up to
        // 5.07 MB; the legacy 2 MB cap silently dropped them. The payload is
        // deliberately NOT WebP: non-WebP bytes pass through untouched, so this
        // pins the byte cap itself (the WebP normalization path has its own
        // tests below).
        var largeIcon = new byte[5 * 1024 * 1024 + 7];
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/7665c31f", HttpStatusCode.OK, largeIcon);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var icon = await service.GetIconAsync(IconUrlFor("7665c31f"));

        // Then
        Assert.NotNull(icon);
        Assert.Equal(largeIcon.Length, icon!.Length);
    }

    private static string IconUrlFor(string id) =>
        $"https://cfx-server-metadata.cfx-services.net/images/{id}";

    [Fact]
    public async Task GetIconAsync_WhenIconExceedsMaxSize_ShouldReturnNull()
    {
        // Given — a decompression-bomb style icon is dropped, never decoded into the cache.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/7665c31f", HttpStatusCode.OK, new byte[500]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, maxIconBytes: 100);

        // When
        var result = await service.GetIconAsync(IconUrlFor("7665c31f"));

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIconAsync_WhenIconUrlUnchanged_ShouldServeCachedIconWithoutRedownload()
    {
        // Given — the icon URL is the complete icon identity since CFX's 2026-10
        // change (a re-uploaded icon gets a new guid URL), so it is the whole cache
        // key: an unchanged URL never re-downloads.
        const string iconUrl = "https://cfx-server-metadata.cfx-services.net/images/7665c31f-234f-4758-bc1a-a24d2eb4f530";
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(iconUrl, HttpStatusCode.OK, [1, 2, 3, 4]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var first = await service.GetIconAsync(iconUrl);
        var second = await service.GetIconAsync(iconUrl);

        // Then
        Assert.Equal([1, 2, 3, 4], first);
        Assert.Equal([1, 2, 3, 4], second);
        Assert.Equal(1, handler.RequestCountByPath("images/7665c31f"));
    }

    [Fact]
    public async Task GetIconAsync_WhenIconUrlChanges_ShouldDownloadNewIcon()
    {
        // Given — a different URL is a different icon; the cache key changes with it.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/7665c31f", HttpStatusCode.OK, [1]);
        handler.AddBytesRoute("images/0af60b69", HttpStatusCode.OK, [2]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var first = await service.GetIconAsync(IconUrlFor("7665c31f"));
        var second = await service.GetIconAsync(IconUrlFor("0af60b69"));

        // Then
        Assert.Equal([1], first);
        Assert.Equal([2], second);
        Assert.Equal(1, handler.RequestCountByPath("images/7665c31f"));
        Assert.Equal(1, handler.RequestCountByPath("images/0af60b69"));
    }

    [Fact]
    public async Task GetIconAsync_WhenIconUrlIsNotSafeHttps_ShouldReturnNullWithoutAnyRequest()
    {
        // Given — the icon URL is server-published, untrusted input: only a
        // validated absolute https URL without credentials is ever downloaded.
        // Rejection must happen BEFORE HttpClient is reached — GetAsync("file://…")
        // throws NotSupportedException, which sits outside the sanctioned swallow
        // set and would fault every caller sharing the in-flight download.
        string[] unsafeUrls =
        [
            "http://cfx-server-metadata.cfx-services.net/images/7665c31f",
            "ftp://example.com/icon.webp",
            "file:///C:/Windows/System32/config.sys",
            "//cfx-server-metadata.cfx-services.net/images/7665c31f",
            "/images/7665c31f",
            "cfx-server-metadata.cfx-services.net/images/7665c31f",
            "https://user:pass@cfx-server-metadata.cfx-services.net/images/7665c31f",
            "not a url",
            "",
            "   "
        ];
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute(
            "cfx-server-metadata.cfx-services.net/images/",
            HttpStatusCode.OK,
            [9, 9, 9]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        foreach (var url in unsafeUrls)
        {
            // When
            var icon = await service.GetIconAsync(url);

            // Then
            Assert.Null(icon);
        }

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetIconAsync_WhenIconUrlServesWebP_ShouldReturnNormalizedPng()
    {
        // Given — CFX's icon host serves WebP, which WPF cannot render; the icon
        // seam must hand normalized, bounded PNG bytes to every caller (row
        // converter LRU, shortcut ICO writer), and cache them in that form.
        var webp = TestWebpEncoder.EncodeRgba(400, 400);
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/7665c31f", HttpStatusCode.OK, webp);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var icon = await service.GetIconAsync(IconUrlFor("7665c31f"));

        // Then — PNG bounded to the 96px headroom.
        Assert.NotNull(icon);
        Assert.Equal(0x89, icon![0]);
        Assert.Equal((byte)'P', icon[1]);
        var frame = System.Windows.Media.Imaging.BitmapFrame.Create(
            new MemoryStream(icon), default, System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
        Assert.Equal((96, 96), (frame.PixelWidth, frame.PixelHeight));

        // And — the cache serves the same normalized instance without a re-download.
        var again = await service.GetIconAsync(IconUrlFor("7665c31f"));
        Assert.Same(icon, again);
        Assert.Equal(1, handler.RequestCountByPath("images/7665c31f"));
    }

    [Fact]
    public async Task GetIconAsync_WhenIconUrlServesUndecodableWebP_ShouldReturnNullWithoutCaching()
    {
        // Given — bytes with a WebP signature but no decodable payload: they must
        // degrade to no icon AND never enter the cache (a later successful upload
        // gets a new URL anyway, but the failed slot must not pin null bytes).
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/7665c31f", HttpStatusCode.OK,
        [
            (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0x00, 0x00, 0x00, 0x00,
            (byte)'W', (byte)'E', (byte)'B', (byte)'P', 0x01, 0x02, 0x03, 0x04
        ]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var icon = await service.GetIconAsync(IconUrlFor("7665c31f"));

        // Then
        Assert.Null(icon);

        // And — nothing was cached: the fetch repeats rather than pinning null.
        Assert.Null(await service.GetIconAsync(IconUrlFor("7665c31f")));
        Assert.Equal(2, handler.RequestCountByPath("images/7665c31f"));
    }

    [Fact]
    public async Task GetIconAsync_WhenIconDownloadFails_ShouldReturnNullWithoutThrowing()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/7665c31f", HttpStatusCode.NotFound, Array.Empty<byte>());
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.GetIconAsync(IconUrlFor("7665c31f"));

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
        var first = service.GetIconAsync(IconUrlFor("7665c31f"));
        var second = service.GetIconAsync(IconUrlFor("7665c31f"));
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
    public async Task GetIconAsync_WhenIconUrlServed_ShouldFetchOnlyTheIconEndpoint()
    {
        // Given — request hygiene: the icon URL is the single icon fact, so fetching
        // an icon must never touch the /single/ endpoint.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/7665c31f", HttpStatusCode.OK, [1, 2, 3]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var icon = await service.GetIconAsync(IconUrlFor("7665c31f"));

        // Then
        Assert.Equal(new byte[] { 1, 2, 3 }, icon);
        Assert.Equal(0, handler.RequestCountByPath("single"));
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
            .Select(_ => service.GetIconAsync(IconUrlFor("7665c31f")))
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
            .Select(i => service.GetIconAsync(IconUrlFor($"srv{i}")))
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
        handler.AddBytesRoute("images/srv0", HttpStatusCode.OK, [10]);
        handler.AddBytesRoute("images/srv1", HttpStatusCode.OK, [11]);
        handler.AddBytesRoute("images/srv2", HttpStatusCode.OK, [12]);
        handler.AddBytesRoute("images/srv3", HttpStatusCode.OK, [13]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, maxCachedIcons: 3);

        // When
        await service.GetIconAsync(IconUrlFor("srv0"));
        await service.GetIconAsync(IconUrlFor("srv1"));
        await service.GetIconAsync(IconUrlFor("srv2"));
        await service.GetIconAsync(IconUrlFor("srv0"));
        await service.GetIconAsync(IconUrlFor("srv3"));
        var evicted = await service.GetIconAsync(IconUrlFor("srv1"));
        var retained = await service.GetIconAsync(IconUrlFor("srv0"));

        // Then — srv1 was evicted (LRU: srv0 was re-touched and survived); srv0 stays cached.
        Assert.Equal(new byte[] { 11 }, evicted);
        Assert.Equal(new byte[] { 10 }, retained);
        Assert.Equal(2, handler.RequestCountByPath("images/srv1"));
        Assert.Equal(1, handler.RequestCountByPath("images/srv0"));
        Assert.Equal(1, handler.RequestCountByPath("images/srv3"));
    }

    [Fact]
    public async Task GetIconAsync_WhenDownloadCompletesSynchronously_ShouldNotParkCompletedTaskAsInFlight()
    {
        // Given — a download can finish before GetIconAsync even registers it (the
        // routed handler answers synchronously); a completed task parked as in-flight
        // would later serve stale bytes with no download after an eviction.
        var handler = new RoutedHttpMessageHandler();
        handler.AddBytesRoute("images/srva", HttpStatusCode.OK, [10]);
        handler.AddBytesRoute("images/srvb", HttpStatusCode.OK, [11]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient, maxCachedIcons: 1);

        // When
        await service.GetIconAsync(IconUrlFor("srva"));
        await service.GetIconAsync(IconUrlFor("srvb"));
        var refetched = await service.GetIconAsync(IconUrlFor("srva"));

        // Then — srva was evicted by srvb, so the re-request downloads again.
        Assert.Equal(new byte[] { 10 }, refetched);
        Assert.Equal(2, handler.RequestCountByPath("images/srva"));
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

