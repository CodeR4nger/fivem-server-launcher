using System.Net;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class CfxServiceTests
{
    [Fact]
    public async Task GetServer_ShouldReturnServerInformation()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
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

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
    }
    [Fact]
    public async Task GetServer_ShouldRequestCfxServerEndpoint()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "hostname": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.Equal(
            "https://frontend.cfx-services.net/api/servers/single/y4lg95",
            handler.LastRequest?.RequestUri?.ToString());
    }
    [Fact]
    public async Task GetServer_ShouldReturnNullWhenCfxReturnsNotFound()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.NotFound,
            string.Empty);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.Null(result);
    }
    [Fact]
    public async Task GetServer_ShouldReturnHostname()
    {
        // Given
        const string cfxId = "y4lg95";
        const string hostname = "Test Server";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            $$"""
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "hostname": "{{hostname}}"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Equal(hostname, result.Hostname);
    }
    [Fact]
    public async Task GetServer_ShouldReturnNullWhenResponseHasNoData()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com"
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.Null(result);
    }
    [Fact]
    public async Task GetServer_ShouldThrowWhenCfxReturnsServerError()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            string.Empty);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When / Then
        await Assert.ThrowsAsync<HttpRequestException>(
            () => cfxService.GetServerAsync(cfxId));
    }
}
