using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.Service;

public class GameBuildDataServiceTests
{
    private const string PremakeLua = """
        return {
            five = {
                game_3889 = "3889_0",
                game_3788 = "3788_0",
                game_2060 = "2060_2",
                game_1604 = "1604_0",
                game_1 = "1", -- Special build that is used to remove all DLCs from the game.
            },
            rdr3 = {
                game_1491 = "1491_50",
                game_1436 = "1436_31",
            },
            ny = {
                game_43 = "43_0",
            }
        }
        """;

    private const string ServersPageHtml = """
        <!DOCTYPE html>
        <html><head><title>Server List</title>
        <script type="module" crossorigin src="/assets/serversList-TESTHASH.js"></script>
        </head><body><div id="cfxui-root"></div></body></html>
        """;

    private const string FrontendBundleJs = """
        var noise=`why hello there`;function other(n){switch(n){case`9`:return`decoy`}}
        function getGameBuildDLCName(n){switch(n){case`2060`:return`Los Santos Summer Special`;case`2189`:case`2215`:case`2245`:return`Cayo Perico Heist`;case`3095`:return`The Chop Shop`;case`1`:return`No DLCs (revised)`}return``}
        var moreNoise=2;
        """;

    private static RoutedHttpMessageHandler AddHappyRoutes()
    {
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("premake5_builds.lua", System.Net.HttpStatusCode.OK, PremakeLua);
        handler.AddTextRoute("servers.fivem.net/servers", System.Net.HttpStatusCode.OK, ServersPageHtml);
        handler.AddTextRoute("serversList-", System.Net.HttpStatusCode.OK, FrontendBundleJs);
        return handler;
    }

    private static GameBuildDataService CreateService(RoutedHttpMessageHandler handler) =>
        new(new HttpClient(handler));

    [Fact]
    public async Task FetchAsync_WhenAllSourcesAvailable_ShouldReturnMergedData()
    {
        // Given
        var handler = AddHappyRoutes();
        var service = CreateService(handler);

        // When
        var result = await service.FetchAsync();

        // Then: numbers come from the premake file, both sections, newest-first as written
        Assert.NotNull(result);
        Assert.Equal([3889, 3788, 2060, 1604, 1], result.FiveM);
        Assert.Equal([1491, 1436], result.RedM);

        // And: the ny section is ignored
        Assert.DoesNotContain(result.FiveM, build => build == 43);
        Assert.DoesNotContain(result.RedM, build => build == 43);

        // And: names come from the bundle, with fallthrough cases grouped to one name
        Assert.Equal("Los Santos Summer Special", result.Names[2060]);
        Assert.Equal("Cayo Perico Heist", result.Names[2189]);
        Assert.Equal("Cayo Perico Heist", result.Names[2215]);
        Assert.Equal("Cayo Perico Heist", result.Names[2245]);
        Assert.Equal("The Chop Shop", result.Names[3095]);

        // And: fetched names win over the baseline on conflict
        Assert.Equal("No DLCs (revised)", result.Names[1]);

        // And: names the bundle does not map keep the baseline label
        Assert.Equal("Arena War", result.Names[1604]);
    }

    [Fact]
    public async Task FetchAsync_WhenPremakeUnreachable_ShouldReturnNull()
    {
        // Given: no routes registered at all - every request 404s
        var handler = new RoutedHttpMessageHandler();
        var service = CreateService(handler);

        // When
        var result = await service.FetchAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_WhenPremakeIsNotLua_ShouldReturnNull()
    {
        // Given
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("premake5_builds.lua", System.Net.HttpStatusCode.OK, "404: Not Found");
        var service = CreateService(handler);

        // When
        var result = await service.FetchAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_WhenPremakeLacksRedMSection_ShouldReturnNull()
    {
        // Given: a premake file without any rdr3 entries
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute(
            "premake5_builds.lua",
            System.Net.HttpStatusCode.OK,
            """
            return {
                five = {
                    game_3095 = "3095_0",
                }
            }
            """);
        var service = CreateService(handler);

        // When
        var result = await service.FetchAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_WhenFrontendPageUnreachable_ShouldReturnNumbersWithBaselineNames()
    {
        // Given: premake reachable, the frontend page is not
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("premake5_builds.lua", System.Net.HttpStatusCode.OK, PremakeLua);
        var service = CreateService(handler);

        // When
        var result = await service.FetchAsync();

        // Then: numbers still refresh; names degrade to the curated baseline
        Assert.NotNull(result);
        Assert.Equal([3889, 3788, 2060, 1604, 1], result.FiveM);
        Assert.Equal([1491, 1436], result.RedM);
        Assert.Equal(GameBuilds.Baseline.Names, result.Names);
    }

    [Fact]
    public async Task FetchAsync_WhenBundleLacksNameFunction_ShouldReturnNumbersWithBaselineNames()
    {
        // Given: the page resolves a bundle that no longer carries the name switch
        var handler = new RoutedHttpMessageHandler();
        handler.AddTextRoute("premake5_builds.lua", System.Net.HttpStatusCode.OK, PremakeLua);
        handler.AddTextRoute("servers.fivem.net/servers", System.Net.HttpStatusCode.OK, ServersPageHtml);
        handler.AddTextRoute("serversList-", System.Net.HttpStatusCode.OK, "function somethingElse(){}");
        var service = CreateService(handler);

        // When
        var result = await service.FetchAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(GameBuilds.Baseline.Names, result.Names);
    }
}
