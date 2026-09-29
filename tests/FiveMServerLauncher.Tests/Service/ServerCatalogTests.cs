using System.Net;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Domain;
using Xunit;
namespace FiveMServerLauncher.Tests.Service;

public class ServerCatalogTests
{
    [Fact]
    public async Task LookupByEndPoint_WhenServerExists_ShouldReturnServer()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData { Hostname = "Test Server" }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupByEndPointAsync("y4lg95");

        // Then
        Assert.NotNull(result);
        Assert.Equal("y4lg95", result.EndPoint);
        Assert.Equal("Test Server", result.Data.Hostname);
    }

    [Fact]
    public async Task LookupByEndPoint_WhenServerMissing_ShouldReturnNull()
    {
        // Given
        var protoServer = new Master.Server { EndPoint = "bbbbbb" };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupByEndPointAsync("y4lg95");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task Lookup_ShouldUseStreamRedirEndpointAndUserAgent()
    {
        // Given
        var protoServer = new Master.Server { EndPoint = "y4lg95" };
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        await catalog.LookupByEndPointAsync("y4lg95");

        // Then
        Assert.Equal(
            "https://frontend.cfx-services.net/api/servers/streamRedir/",
            handler.LastRequest?.RequestUri?.ToString());
        Assert.NotNull(handler.LastRequest?.Headers.UserAgent);
        Assert.NotEmpty(handler.LastRequest.Headers.UserAgent);
    }

    [Fact]
    public async Task Lookup_WhenCacheFresh_ShouldNotRedownload()
    {
        // Given
        var protoServer = new Master.Server { EndPoint = "y4lg95" };
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var catalog = new ServerCatalog(httpClient, clock);

        // When
        await catalog.LookupByEndPointAsync("y4lg95");
        await catalog.LookupByEndPointAsync("y4lg95");

        // Then
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Lookup_WhenCacheExpired_ShouldRedownload()
    {
        // Given
        var protoServer = new Master.Server { EndPoint = "y4lg95" };
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var catalog = new ServerCatalog(httpClient, clock);

        // When
        await catalog.LookupByEndPointAsync("y4lg95");
        clock.Advance(TimeSpan.FromMinutes(6));
        await catalog.LookupByEndPointAsync("y4lg95");

        // Then
        Assert.Equal(2, handler.RequestCount);
    }

[Fact]
    public async Task Lookup_WhenCatalogUnavailable_ShouldReturnNull()
    {
        // Given
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            Array.Empty<byte>());
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupByEndPointAsync("y4lg95");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task Lookup_WhenNetworkFails_ShouldReturnNull()
    {
        // Given
        var handler = new FakeHttpMessageHandler(true);
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupByEndPointAsync("y4lg95");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task Lookup_WhenUnexpectedException_ShouldPropagate()
    {
        // Given
        using var httpClient = new HttpClient(new CrashedHttpMessageHandler());
        var catalog = new ServerCatalog(httpClient);

        // When / Then
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => catalog.LookupByEndPointAsync("y4lg95"));
    }

    private sealed class CrashedHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated programming error");
        }
    }

    [Fact]
    public async Task GetSnapshot_WhenPayloadExceedsMaxSize_ShouldDegradeToOutage()
    {
        // Given — an oversized payload is treated like an outage, never decoded or cached.
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            new byte[50]);
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient, maxPayloadBytes: 10);

        // When
        var result = await catalog.GetSnapshotAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupByIpPort_WhenServerListed_ShouldReturnServer()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "149.56.120.52:30320", "play.example.com:30120" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupByIpPortAsync("149.56.120.52:30320");

        // Then
        Assert.NotNull(result);
        Assert.Equal("y4lg95", result.EndPoint);
    }

    [Fact]
    public async Task LookupByIpPort_WhenOnlyLoopbackEndpointMatches_ShouldReturnNull()
    {
        // Given — a listed hidden server publishes a loopback endpoint (e.g. 8y6354);
        // typed local addresses must never resolve to a stranger's hidden server.
        var protoServer = new Master.Server
        {
            EndPoint = "8y6354",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "127.0.0.1:30120" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupByIpPortAsync("127.0.0.1:30120");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupByIpPort_WhenServerHasPublicAndLoopbackEndpoints_ShouldMatchPublicOnly()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "149.56.120.52:30320", "127.0.0.1:30120" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When / Then
        Assert.NotNull(await catalog.LookupByIpPortAsync("149.56.120.52:30320"));
        Assert.Null(await catalog.LookupByIpPortAsync("127.0.0.1:30120"));
    }

    [Fact]
    public async Task LookupBareIp_WhenOnlyLoopbackEndpointListed_ShouldReturnNull()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "8y6354",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "127.0.0.1:30120" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupBareIpAsync("127.0.0.1");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupBareDomain_WhenDnsResolvesToLoopbackListedEndpoint_ShouldReturnNull()
    {
        // Given — `localhost` resolves to the loopback IP, but the only catalog entry behind
        // it is a hidden server; the lookup must degrade to no match.
        var protoServer = new Master.Server
        {
            EndPoint = "8y6354",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "127.0.0.1:30120" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient, dnsResolver: new FakeDnsResolver("127.0.0.1"));

        // When
        var result = await catalog.LookupBareDomainAsync("localhost");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByEndpointHost_WhenHostIsLoopback_ShouldNotMatch()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "8y6354",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "localhost:30120" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.FindByEndpointHostAsync("localhost");

        // Then
        Assert.Empty(result);
    }

    [Fact]
    public async Task LookupBareDomain_WhenSentinelHostTyped_ShouldReturnNull()
    {
        // Given — the hidden-server sentinel host is never matchable by address either.
        var protoServer = new Master.Server
        {
            EndPoint = "r8q73g",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "https://private-placeholder.cfx.re/" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient, dnsResolver: new FakeDnsResolver("127.0.0.1"));

        // When
        var result = await catalog.LookupBareDomainAsync("private-placeholder.cfx.re");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task LookupByIpPort_WhenServerMissing_ShouldReturnNull()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "149.56.120.52:30320" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.LookupByIpPortAsync("10.0.0.1:9999");

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task FindByEndpointHost_WhenProxyUrlEndpointMatches_ShouldReturnServer()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "https://play.example.com:443/" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.FindByEndpointHostAsync("play.example.com");

        // Then
        var server = Assert.Single(result);
        Assert.Equal("y4lg95", server.EndPoint);
    }

    [Fact]
    public async Task FindByEndpointHost_WhenPlainHostPortEndpointMatches_ShouldReturnServer()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "play.example.com:30120" }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.FindByEndpointHostAsync("PLAY.EXAMPLE.COM");

        // Then
        Assert.Single(result);
    }

    [Fact]
    public async Task FindByEndpointHost_WhenHostSharedByMultipleServers_ShouldReturnAll()
    {
        // Given
        var first = new Master.Server
        {
            EndPoint = "aaaaaa",
            Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io/" } }
        };
        var second = new Master.Server
        {
            EndPoint = "bbbbbb",
            Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io:443/" } }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.Join(
                TestProtobufFrames.BuildFrameStream(first),
                TestProtobufFrames.BuildFrameStream(second)));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.FindByEndpointHostAsync("proxy.shared.io");

        // Then
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FindByEndpointHost_WhenNoMatch_ShouldReturnEmpty()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30120" } }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var catalog = new ServerCatalog(httpClient);

        // When
        var result = await catalog.FindByEndpointHostAsync("play.example.com");

        // Then
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSnapshot_WhenForcedInsideTtl_ShouldRefetch()
    {
        // Given
        var protoServer = new Master.Server { EndPoint = "y4lg95" };
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var catalog = new ServerCatalog(httpClient, clock);

        // When
        await catalog.GetSnapshotAsync();
        var forced = await catalog.GetSnapshotAsync(forceRefresh: true);

        // Then
        Assert.Equal(2, handler.RequestCount);
        Assert.NotNull(forced);
    }

    [Fact]
    public async Task GetSnapshot_WhenForcedFetchFailsWithWarmCache_ShouldReturnStaleCache()
    {
        // Given — handler route becomes unavailable after the first request
        var handler = new FlakyOnceHttpMessageHandler(
            TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "y4lg95" }));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var catalog = new ServerCatalog(httpClient, clock);

        // When
        var first = await catalog.GetSnapshotAsync();
        var second = await catalog.GetSnapshotAsync(forceRefresh: true);

        // Then
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Same(first, second);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetSnapshot_WhenConcurrentCallsWithColdCache_ShouldShareOneInFlightDownload()
    {
        // Given — the multi-megabyte catalog must be downloaded once no matter how many
        // callers hit a cold cache at the same time (connect, enrichment loop, browser).
        var handler = new GatedHttpMessageHandler(
            TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "y4lg95" }));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var catalog = new ServerCatalog(httpClient, clock);

        // When
        var first = catalog.GetSnapshotAsync();
        var second = catalog.GetSnapshotAsync();
        Assert.Equal(1, handler.RequestCount);
        handler.Release();
        var firstResult = await first;
        var secondResult = await second;

        // Then
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.Same(firstResult, secondResult);
        Assert.Equal(1, handler.RequestCount);

        var cached = await catalog.GetSnapshotAsync();
        Assert.Same(firstResult, cached);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetSnapshot_WhenForcedCallOverlapsInFlightFetch_ShouldJoinIt()
    {
        // Given — a forced refresh arriving while a download is in flight reuses it;
        // the in-flight download is fresher than any cache could be.
        var handler = new GatedHttpMessageHandler(
            TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "y4lg95" }));
        using var httpClient = new HttpClient(handler);
        var clock = new FakeTimeProvider();
        var catalog = new ServerCatalog(httpClient, clock);

        // When
        var inFlight = catalog.GetSnapshotAsync();
        var forced = catalog.GetSnapshotAsync(forceRefresh: true);
        Assert.Equal(1, handler.RequestCount);
        handler.Release();
        var inFlightResult = await inFlight;
        var forcedResult = await forced;

        // Then
        Assert.NotNull(forcedResult);
        Assert.Same(inFlightResult, forcedResult);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class FlakyOnceHttpMessageHandler(byte[] payload) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            if (RequestCount > 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            });
        }
    }

    private sealed class GatedHttpMessageHandler(byte[] payload) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _gate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requests;

        public int RequestCount => Volatile.Read(ref _requests);

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
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            };
        }
    }
}
