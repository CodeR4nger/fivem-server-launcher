using System.Net;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

public class CfxServiceTests
{
    [Fact]
    public async Task GetServer_WhenEndPointDiffersFromRequestedId_ShouldUseRequestedCfxId()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Equal(cfxId, result.CfxId);
    }
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
                    "sv_projectName": "Test Server"
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
                    "sv_projectName": "Test Server"
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
    public async Task GetServer_ShouldReturnProjectName()
    {
        // Given
        const string cfxId = "y4lg95";
        const string ProjectName = "Test Server";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            $$"""
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "{{ProjectName}}"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Equal(ProjectName, result.ProjectName);
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
    public async Task GetServer_ShouldReturnTypedServerVariables()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_enforceGameBuild": "3258",
                        "sv_pureLevel": "1",
                        "gamename": "gta5enhanced"
                    },
                    "requestSteamTicket":"on"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Equal(3258, result.EnforceGameBuild);
        Assert.Equal(1, result.PureLevel);
        Assert.Equal(GameClient.FiveMEnhanced, result.GameClient);
        Assert.True(result.RequestSteamTicket);
    }

    [Fact]
    public async Task GetServer_ShouldReturnNullForInvalidTypedVariables()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_enforceGameBuild": "banana",
                        "sv_pureLevel": "invalid",
                        "gamename": "gta4"
                    },
                    "requestSteamTicket":"dfer"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Null(result.EnforceGameBuild);
        Assert.Null(result.PureLevel);
        Assert.Null(result.RequestSteamTicket);
        Assert.Null(result.GameClient);
    }

    [Fact]
    public async Task GetServer_WhenRequestSteamTicketOff_ShouldReturnFalse()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "requestSteamTicket": "off"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.False(result.RequestSteamTicket);
    }

    [Fact]
    public async Task GetServer_WhenRequestSteamTicketAbsent_ShouldReturnNull()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Null(result.RequestSteamTicket);
    }

    [Fact]
    public async Task GetServer_ShouldReturnCitizenFxConfigVariables()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_defaultGameBuild": "3788",
                        "sv_replaceExeToSwitchBuilds": "true",
                        "sv_poolSizesIncrease": "{\"CWeaponComponentInfo\":500}"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Equal(3788, result.DefaultGameBuild);
        Assert.True(result.ReplaceExecutableToSwitchBuilds);
        Assert.Equal("{\"CWeaponComponentInfo\":500}", result.PoolSizesIncrease);
    }

    [Fact]
    public async Task GetServer_WhenCitizenFxConfigVariablesInvalid_ShouldReturnNull()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_defaultGameBuild": "banana",
                        "sv_replaceExeToSwitchBuilds": "yes"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Null(result.DefaultGameBuild);
        Assert.Null(result.ReplaceExecutableToSwitchBuilds);
        Assert.Null(result.PoolSizesIncrease);
    }

    [Fact]
    public async Task GetServer_WhenEnforceSteamAuthSet_ShouldReturnSteamEnforced()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_enforceSteamAuth": "true"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.True(result.SteamEnforced);
    }

    [Fact]
    public async Task GetServer_WhenEnforceSteamAuthFalse_ShouldReturnFalse()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_enforceSteamAuth": "false"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.False(result.SteamEnforced);
    }

    [Fact]
    public async Task GetServer_WhenEnforceSteamAuthAbsentOrInvalid_ShouldReturnNull()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            """
            {
                "EndPoint": "https://example.com",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_enforceSteamAuth": "banana"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);

        // When
        var result = await cfxService.GetServerAsync(cfxId);

        // Then
        Assert.NotNull(result);
        Assert.Null(result.SteamEnforced);
    }

}
