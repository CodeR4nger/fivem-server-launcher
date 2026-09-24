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
    public async Task GetIconAsync_WhenSingleResponsePayloadIsCorrupt_ShouldReturnNull()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("single/y4lg95", HttpStatusCode.OK, "not-json{{{");
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.GetIconAsync("y4lg95");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIconAsync_WhenVersionUnchanged_ShouldServeCachedIconWithoutRedownload()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("single/y4lg95", HttpStatusCode.OK, """{"data":{"iconVersion":288154985}}""");
        handler.AddBytesRoute("icon/y4lg95/288154985.png", HttpStatusCode.OK, [1, 2, 3, 4]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var first = await service.GetIconAsync("y4lg95");
        var second = await service.GetIconAsync("y4lg95");

        // Then
        Assert.Equal([1, 2, 3, 4], first);
        Assert.Equal([1, 2, 3, 4], second);
        Assert.Equal(2, handler.RequestCountByPath("single/y4lg95"));
        Assert.Equal(1, handler.RequestCountByPath("icon/y4lg95/288154985.png"));
    }

    [Fact]
    public async Task GetIconAsync_WhenVersionChanges_ShouldDownloadNewIcon()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("single/y4lg95", HttpStatusCode.OK, """{"data":{"iconVersion":1}}""");
        handler.AddBytesRoute("icon/y4lg95/1.png", HttpStatusCode.OK, [1]);
        handler.AddBytesRoute("icon/y4lg95/2.png", HttpStatusCode.OK, [2]);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var first = await service.GetIconAsync("y4lg95");
        handler.AddTextRoute("single/y4lg95", HttpStatusCode.OK, """{"data":{"iconVersion":2}}""");
        var second = await service.GetIconAsync("y4lg95");

        // Then
        Assert.Equal([1], first);
        Assert.Equal([2], second);
        Assert.Equal(1, handler.RequestCountByPath("icon/y4lg95/1.png"));
        Assert.Equal(1, handler.RequestCountByPath("icon/y4lg95/2.png"));
    }

    [Fact]
    public async Task GetIconAsync_WhenSingleEndpointUnavailable_ShouldReturnNullWithoutThrowing()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("single/y4lg95", HttpStatusCode.NotFound, string.Empty);
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.GetIconAsync("y4lg95");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetIconAsync_WhenIconDownloadFails_ShouldReturnNullWithoutThrowing()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("single/y4lg95", HttpStatusCode.OK, """{"data":{"iconVersion":5}}""");
        handler.AddBytesRoute("icon/y4lg95/5.png", HttpStatusCode.NotFound, Array.Empty<byte>());
        using var httpClient = new HttpClient(handler);
        var service = new ServerEnrichmentService(httpClient);

        // When
        var result = await service.GetIconAsync("y4lg95");

        // Then
        Assert.Null(result);
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

    private sealed class HttpMessageHandlerStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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

