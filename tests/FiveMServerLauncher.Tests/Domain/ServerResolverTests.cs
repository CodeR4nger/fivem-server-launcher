using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Service;
namespace FiveMServerLauncher.Tests.Domain;

public class ServerResolverTests
{
    [Fact]
    public async Task Resolve_WhenValidCfxId_ShouldReturnCfxId()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "hostname": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = new ServerResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(cfxId);

        // Then
        Assert.Equal(cfxId, result.CfxId);
    }
    [Fact]
    public async Task Resolve_WhenInvalidAddress_ShouldThrow()
    {
        // Given
        const string address = "unknown";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.NotFound,
            string.Empty);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = new ServerResolver(cfxService);

        // When / Then
        await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address));
    }
    [Fact]
    public async Task Resolve_WhenEmptyAddress_ShouldThrow()
    {
        // Given
        const string address = "   ";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "hostname": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = new ServerResolver(cfxService);

        // When / Then
        await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address));
    }
    [Fact]
    public async Task Resolve_WhenCfxUrl_ShouldUseCfxIdFromUrl()
    {
        // Given
        const string address = "https://cfx.re/join/y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "hostname": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = new ServerResolver(cfxService);

        // When
        await resolver.ResolveAsync(address);

        // Then
        Assert.Equal(
            "https://frontend.cfx-services.net/api/servers/single/y4lg95",
            handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task Resolve_WhenValidCfxUrl_ShouldReturnCfxId()
    {
        // Given
        const string address = "https://cfx.re/join/y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "hostname": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = new ServerResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.Equal("y4lg95", result.CfxId);
    }
    [Fact]
    public async Task Resolve_ShouldReturnCfxIdForCfxJoinUrlWithoutScheme()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "hostname": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = new ServerResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.Equal(
            "https://frontend.cfx-services.net/api/servers/single/y4lg95",
            handler.LastRequest?.RequestUri?.ToString());
    }

}