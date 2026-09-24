using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

public class ServerBrowserItemTests
{
    [Fact]
    public void FromServer_ShouldPreferProjectNameAndStripColorCodes()
    {
        // Given
        var server = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Hostname = "^6Raw ^1Hostname",
                Clients = 12,
                SvMaxclients = 48,
                Vars =
                {
                    ["sv_projectName"] = "^2Cool ^9Server",
                    ["gamename"] = "gta5"
                },
                ConnectEndPoints = { "149.56.120.52:30120" }
            }
        };

        // When
        var item = ServerBrowserItem.FromServer(server);

        // Then
        Assert.Equal("Cool Server", item.Name);
        Assert.Equal("y4lg95", item.CfxId);
        Assert.Equal(GameClient.FiveM, item.Game);
        Assert.Equal(12, item.Players);
        Assert.Equal(48, item.MaxPlayers);
        Assert.Equal("149.56.120.52:30120", item.Address);
    }

    [Fact]
    public void FromServer_WhenNoProjectName_ShouldFallBackToHostnameStripped()
    {
        // Given
        var server = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Hostname = "^6Midnight ^9Roleplay^r",
                ConnectEndPoints = { "149.56.120.52:30120" }
            }
        };

        // When
        var item = ServerBrowserItem.FromServer(server);

        // Then
        Assert.Equal("Midnight Roleplay", item.Name);
    }

    [Fact]
    public void FromServer_WhenGamenameUnknown_ShouldLeaveGameNull()
    {
        // Given
        var server = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "149.56.120.52:30120" },
                Vars = { ["gamename"] = "not-a-game" }
            }
        };

        // When
        var item = ServerBrowserItem.FromServer(server);

        // Then
        Assert.Null(item.Game);
    }

    [Fact]
    public void FromServer_WhenHiddenSentinel_ShouldUseCfxJoinAddress()
    {
        // Given
        var server = new Master.Server
        {
            EndPoint = "r8q73g",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "https://private-placeholder.cfx.re/" }
            }
        };

        // When
        var item = ServerBrowserItem.FromServer(server);

        // Then
        Assert.Equal("cfx.re/join/r8q73g", item.Address);
    }

    [Fact]
    public void FromServer_WhenNoEndpoint_ShouldUseCfxJoinAddress()
    {
        // Given
        var server = new Master.Server
        {
            EndPoint = "r8q73g",
            Data = new Master.ServerData()
        };

        // When
        var item = ServerBrowserItem.FromServer(server);

        // Then
        Assert.Equal("cfx.re/join/r8q73g", item.Address);
    }
}
