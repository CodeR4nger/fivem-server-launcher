using FiveMServerLauncher.Service;
using System.IO;
using Xunit;
namespace FiveMServerLauncher.Tests.Service;

public class ServerCatalogDecoderTests
{
    [Fact]
    public async Task DecodeAsync_WhenSingleFrame_ShouldReturnOneServer()
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
        var servers = await DecodeTrickled(stream);

        // Then
        var server = Assert.Single(servers!);
        Assert.Equal("y4lg95", server.EndPoint);
        Assert.Equal("Test Server", server.Data.Hostname);
    }

    [Fact]
    public async Task DecodeAsync_WhenMultipleFrames_ShouldReturnAllServers()
    {
        // Given
        var first = new Master.Server { EndPoint = "aaaaaa", Data = new Master.ServerData { Hostname = "First" } };
        var second = new Master.Server { EndPoint = "bbbbbb", Data = new Master.ServerData { Hostname = "Second" } };

        var stream = TestProtobufFrames.Join(TestProtobufFrames.BuildFrameStream(first), TestProtobufFrames.BuildFrameStream(second));

        // When
        var servers = await DecodeTrickled(stream);

        // Then
        Assert.Equal(2, servers!.Count);
        Assert.Equal(["aaaaaa", "bbbbbb"], servers!.Select(s => s.EndPoint));
    }

    [Fact]
    public async Task DecodeAsync_WhenServerHasConnectEndPoints_ShouldPreserveThem()
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
        var servers = await DecodeTrickled(stream);

        // Then
        var server = Assert.Single(servers!);
        Assert.Equal(
            ["149.56.120.52:30320", "play.example.com:30120"],
            server.Data.ConnectEndPoints);
    }

    [Fact]
    public async Task DecodeAsync_WhenCorruptFrameBetweenValidOnes_ShouldSkipItAndKeepTheRest()
    {
        // Given
        var first = new Master.Server { EndPoint = "aaaaaa", Data = new Master.ServerData { Hostname = "First" } };
        var last = new Master.Server { EndPoint = "cccccc", Data = new Master.ServerData { Hostname = "Last" } };

        var corruptFrame = new byte[4 + 5]; // plausible length prefix + garbage payload (not a valid protobuf)
        corruptFrame[0] = 5;
        Array.Fill(corruptFrame, (byte)0xFF, 4, 5);

        var stream = TestProtobufFrames.Join(TestProtobufFrames.BuildFrameStream(first), corruptFrame, TestProtobufFrames.BuildFrameStream(last));

        // When
        var servers = await DecodeTrickled(stream);

        // Then
        Assert.Equal(["aaaaaa", "cccccc"], servers!.Select(s => s.EndPoint));
    }

    [Fact]
    public async Task DecodeAsync_WhenTruncatedTrailingFrame_ShouldIgnoreIt()
    {
        // Given
        var corruptFrame = new byte[4 + 5];
        Array.Fill(corruptFrame, (byte)0xFF); // length that overruns the buffer

        // When
        var servers = await DecodeTrickled(corruptFrame);

        // Then
        Assert.Empty(servers!);
    }

    [Fact]
    public async Task DecodeAsync_WhenServerHasVars_ShouldPreserveTypedFieldsTheResolverUses()
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
        var servers = await DecodeTrickled(stream);

        // Then
        var server = Assert.Single(servers!);
        Assert.Equal("y4lg95", server.EndPoint);
        Assert.Equal("gta5", server.Data.Vars["gamename"]);
        Assert.Equal("3095", server.Data.Vars["sv_enforceGameBuild"]);
        Assert.Equal("1", server.Data.Vars["sv_pureLevel"]);
        Assert.Equal("on", server.Data.Vars["requestSteamTicket"]);
    }

    [Fact]
    public async Task DecodeAsync_WhenBodyEndsInsideTheLengthPrefix_ShouldKeepTheCompleteFrames()
    {
        // Given — one whole frame, then two bytes of the next length prefix
        var first = new Master.Server { EndPoint = "aaaaaa" };
        var body = TestProtobufFrames.Join(TestProtobufFrames.BuildFrameStream(first), [0x10, 0x00]);

        // When
        var servers = await DecodeTrickled(body);

        // Then
        Assert.Equal(["aaaaaa"], servers!.Select(s => s.EndPoint));
    }

    [Fact]
    public async Task DecodeAsync_WhenBodyEndsInsideAFrame_ShouldKeepTheCompleteFrames()
    {
        // Given — one whole frame, then a frame cut short by the end of the body
        var first = new Master.Server { EndPoint = "aaaaaa" };
        var second = TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "bbbbbb" });
        var body = TestProtobufFrames.Join(TestProtobufFrames.BuildFrameStream(first), second[..^2]);

        // When
        var servers = await DecodeTrickled(body);

        // Then
        Assert.Equal(["aaaaaa"], servers!.Select(s => s.EndPoint));
    }

    [Fact]
    public async Task DecodeAsync_WhenBodyExceedsTheCap_ShouldReturnNull()
    {
        // Given — valid frames whose total size is over the cap
        var body = TestProtobufFrames.Join(
            TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "aaaaaa" }),
            TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "bbbbbb" }));

        // When
        var servers = await DecodeTrickled(body, maxBytes: body.Length - 1);

        // Then — over the cap is an outage, never a partial catalog
        Assert.Null(servers);
    }

    [Fact]
    public async Task DecodeAsync_WhenBodyIsExactlyTheCap_ShouldDecodeIt()
    {
        // Given
        var body = TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "aaaaaa" });

        // When
        var servers = await DecodeTrickled(body, maxBytes: body.Length);

        // Then
        Assert.Equal(["aaaaaa"], servers!.Select(s => s.EndPoint));
    }

    [Fact]
    public async Task DecodeAsync_WhenFrameClaimsAHugeLength_ShouldNotAllocateIt()
    {
        // Given — a length prefix claiming ~2 GB, followed by a few bytes only
        var body = new byte[4 + 16];
        BitConverter.GetBytes(0x7FFF_FFF0u).CopyTo(body, 0);
        using var source = new MemoryStream(body);

        // When — a MemoryStream completes synchronously, so the thread counter sees it all
        var before = GC.GetAllocatedBytesForCurrentThread();
        var servers = await ServerCatalogDecoder.DecodeAsync(source, long.MaxValue, CancellationToken.None);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Then — truncated frame: nothing decoded, and the claim was never pre-allocated
        Assert.Empty(servers!);
        Assert.True(allocated < 1024 * 1024, $"allocated {allocated} bytes for a lying length prefix");
    }

    [Fact]
    public async Task DecodeAsync_WhenCancelled_ShouldThrowOperationCanceled()
    {
        // Given
        var body = TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "aaaaaa" });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // When / Then
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ServerCatalogDecoder.DecodeAsync(new TrickleStream(body, 3), long.MaxValue, cts.Token));
    }

    // Every decode reads through a stream that hands out a few bytes per read, so length
    // prefixes and frames always straddle reads — the shape of a real network body.
    private static Task<IReadOnlyList<Master.Server>?> DecodeTrickled(byte[] body, long maxBytes = long.MaxValue)
    {
        return ServerCatalogDecoder.DecodeAsync(new TrickleStream(body, 3), maxBytes, CancellationToken.None);
    }

    private sealed class TrickleStream(byte[] data, int chunk) : Stream
    {
        private int _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            var count = Math.Min(Math.Min(chunk, buffer.Length), data.Length - _position);
            data.AsSpan(_position, count).CopyTo(buffer);
            _position += count;
            return count;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            return Read(buffer.Span);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}