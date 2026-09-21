using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Domain;

public class ServerRequirementsResolverTests
{
    [Fact]
    public void Resolve_ShouldReturnGameBuild()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            EnforceGameBuild = 3258,
            GameClient = Core.Enums.GameClient.FiveM,
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.Equal(3258, result.GameBuild);
    }

    [Fact]
    public void Resolve_ShouldReturnPureMode()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            PureLevel = 1,
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.Equal(1, result.PureMode);
    }

    [Fact]
    public void Resolve_ShouldReturnRequestSteamTicket()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            RequestSteamTicket = true,
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.True(result.RequestSteamTicket);
    }

    [Fact]
    public void Resolve_WhenDefaultGameBuildPublished_ShouldReturnDefaultBuild()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            DefaultGameBuild = 3788,
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.Equal(3788, result.DefaultBuild);
    }

    [Fact]
    public void Resolve_WhenReplaceExecutablePublished_ShouldReturnReplaceExecutable()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            ReplaceExecutableToSwitchBuilds = true,
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.True(result.ReplaceExecutable);
    }

    [Fact]
    public void Resolve_WhenPoolSizesIncreasePublished_ShouldReturnPoolSizesIncrease()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            PoolSizesIncrease = "{\"CWeaponComponentInfo\":500}",
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.Equal("{\"CWeaponComponentInfo\":500}", result.PoolSizesIncrease);
    }

    [Fact]
    public void Resolve_WhenRequirementsNotPublished_ShouldReturnNullFields()
    {
        // Given
        var serverInfo = new CfxServerInfo
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
        };

        var resolver = new ServerRequirementsResolver();

        // When
        var result = resolver.Resolve(serverInfo);

        // Then
        Assert.Null(result.GameBuild);
        Assert.Null(result.PureMode);
        Assert.Null(result.RequestSteamTicket);
        Assert.Null(result.DefaultBuild);
        Assert.Null(result.ReplaceExecutable);
        Assert.Null(result.PoolSizesIncrease);
    }

}