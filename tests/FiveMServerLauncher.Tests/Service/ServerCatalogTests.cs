using System.Net;
using FiveMServerLauncher.Service;
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
}