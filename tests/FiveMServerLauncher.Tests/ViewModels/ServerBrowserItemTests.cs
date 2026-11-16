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
    public void FromServer_WhenEndpointIsLoopback_ShouldUseCfxJoinAddress()
    {
        // Given — a listed hidden server publishing a raw loopback endpoint stays id-only.
        var server = new Master.Server
        {
            EndPoint = "8y6354",
            Data = new Master.ServerData
            {
                ConnectEndPoints = { "127.0.0.1:30120" }
            }
        };

        // When
        var item = ServerBrowserItem.FromServer(server);

        // Then
        Assert.Equal("cfx.re/join/8y6354", item.Address);
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

    [Fact]
    public void PlayersText_ShouldShowPlayersOverMaxPlayers()
    {
        // Given a server with 12 of 48 players
        var item = ServerBrowserItem.FromServer(new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData { Clients = 12, SvMaxclients = 48 }
        });

        // When / Then the row's count reads players/max (computed once, no per-row converter)
        Assert.Equal("12/48", item.PlayersText);
    }

    [Theory]
    [InlineData("gta5", "FiveM", true)]
    [InlineData("gta5enhanced", "FiveM Enhanced", true)]
    [InlineData("rdr3", "RedM", true)]
    [InlineData(null, "", false)]
    public void GameTagLabel_ShouldNameTheGameOrStayEmptyWhenUnknown(string? gameName, string expected, bool hasGame)
    {
        // Given a server publishing the game name (or none)
        var data = new Master.ServerData();

        if (gameName is not null)
        {
            data.Vars["gamename"] = gameName;
        }

        // When
        var item = ServerBrowserItem.FromServer(new Master.Server { EndPoint = "y4lg95", Data = data });

        // Then the filled tag shows the game's display name, and only when the game is known
        Assert.Equal(expected, item.GameTagLabel);
        Assert.Equal(hasGame, item.HasGame);
    }
}
