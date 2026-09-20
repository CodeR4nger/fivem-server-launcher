using FiveMServerLauncher.Service;
using Google.Protobuf;
using Xunit;
namespace FiveMServerLauncher.Tests.Service;

public class ServerCatalogDecoderTests
{
    [Fact]
    public void Decode_WhenSingleFrame_ShouldReturnOneServer()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Hostname = "Test Server",
                Vars = { ["gamename"] = "gta5", ["sv_projectName"] = "Test Server" }
            }
        };

        var stream = TestProtobufFrames.BuildFrameStream(protoServer);

        // When
        var servers = ServerCatalogDecoder.Decode(stream);

        // Then
        var server = Assert.Single(servers);
        Assert.Equal("y4lg95", server.EndPoint);
        Assert.Equal("Test Server", server.Data.Hostname);
    }

    [Fact]
    public void Decode_WhenMultipleFrames_ShouldReturnAllServers()
    {
        // Given
        var first = new Master.Server { EndPoint = "aaaaaa", Data = new Master.ServerData { Hostname = "First" } };
        var second = new Master.Server { EndPoint = "bbbbbb", Data = new Master.ServerData { Hostname = "Second" } };

        var stream = TestProtobufFrames.Join(TestProtobufFrames.BuildFrameStream(first), TestProtobufFrames.BuildFrameStream(second));

        // When
        var servers = ServerCatalogDecoder.Decode(stream);

        // Then
        Assert.Equal(2, servers.Count);
        Assert.Equal(["aaaaaa", "bbbbbb"], servers.Select(s => s.EndPoint));
    }

    [Fact]
    public void Decode_WhenServerHasConnectEndPoints_ShouldPreserveThem()
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

        var stream = TestProtobufFrames.BuildFrameStream(protoServer);

        // When
        var servers = ServerCatalogDecoder.Decode(stream);

        // Then
        var server = Assert.Single(servers);
        Assert.Equal(
            ["149.56.120.52:30320", "play.example.com:30120"],
            server.Data.ConnectEndPoints);
    }

    [Fact]
    public void Decode_WhenCorruptFrameBetweenValidOnes_ShouldSkipItAndKeepTheRest()
    {
        // Given
        var first = new Master.Server { EndPoint = "aaaaaa", Data = new Master.ServerData { Hostname = "First" } };
        var last = new Master.Server { EndPoint = "cccccc", Data = new Master.ServerData { Hostname = "Last" } };

        var corruptFrame = new byte[4 + 5]; // plausible length prefix + garbage payload (not a valid protobuf)
        corruptFrame[0] = 5;
        Array.Fill(corruptFrame, (byte)0xFF, 4, 5);

        var stream = TestProtobufFrames.Join(TestProtobufFrames.BuildFrameStream(first), corruptFrame, TestProtobufFrames.BuildFrameStream(last));

        // When
        var servers = ServerCatalogDecoder.Decode(stream);

        // Then
        Assert.Equal(["aaaaaa", "cccccc"], servers.Select(s => s.EndPoint));
    }

    [Fact]
    public void Decode_WhenTruncatedTrailingFrame_ShouldIgnoreIt()
    {
        // Given
        var corruptFrame = new byte[4 + 5];
        Array.Fill(corruptFrame, (byte)0xFF); // length that overruns the buffer

        // When
        var servers = ServerCatalogDecoder.Decode(corruptFrame);

        // Then
        Assert.Empty(servers);
    }

    [Fact]
    public void Decode_WhenServerHasVars_ShouldPreserveTypedFieldsTheResolverUses()
    {
        // Given
        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Hostname = "Servidor",
                SvMaxclients = 64,
                Clients = 12,
                Server = "FXServer",
                Vars =
                {
                    ["gamename"] = "gta5",
                    ["sv_projectName"] = "Servidor",
                    ["sv_enforceGameBuild"] = "3095",
                    ["sv_pureLevel"] = "1",
                    ["requestSteamTicket"] = "on"
                }
            }
        };

        var stream = TestProtobufFrames.BuildFrameStream(protoServer);

        // When
        var servers = ServerCatalogDecoder.Decode(stream);

        // Then
        var server = Assert.Single(servers);
        Assert.Equal("y4lg95", server.EndPoint);
        Assert.Equal("gta5", server.Data.Vars["gamename"]);
        Assert.Equal("3095", server.Data.Vars["sv_enforceGameBuild"]);
        Assert.Equal("1", server.Data.Vars["sv_pureLevel"]);
        Assert.Equal("on", server.Data.Vars["requestSteamTicket"]);
    }
}