using System.IO;
using Google.Protobuf;

namespace FiveMServerLauncher.Service;

/// <summary>
/// Decodes the streamRedir catalog body (frames of uint32 LE length + protobuf
/// <c>master.Server</c>) straight from the response stream, so the ~20 MB body is never
/// held in memory: the only payload buffer is one frame (the largest real frame is ~12 KB).
/// </summary>
public static class ServerCatalogDecoder
{
    private const int InitialFrameBufferBytes = 16 * 1024;

    /// <summary>
    /// Truncated trailing length prefix or frame → the frames decoded so far; a corrupt
    /// frame is skipped; more than <paramref name="maxBytes"/> body bytes → null (an
    /// outage, never a partial catalog). A claimed frame length is never pre-allocated:
    /// the frame buffer grows only as bytes actually arrive.
    /// </summary>
    public static async Task<IReadOnlyList<Master.Server>?> DecodeAsync(
        Stream source,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var reader = new CappedReader(source, maxBytes);
        var servers = new List<Master.Server>();
        var prefix = new byte[sizeof(uint)];
        var frame = new byte[InitialFrameBufferBytes];

        while (true)
        {
            var prefixRead = await reader.FillAsync(prefix, 0, prefix.Length, cancellationToken);

            if (reader.OverCap)
            {
                return null;
            }

            if (prefixRead < prefix.Length)
            {
                return servers; // end of body, or a truncated length prefix at the end
            }

            var length = BitConverter.ToUInt32(prefix, 0);
            var filled = 0;

            while (filled < length)
            {
                if (filled == frame.Length)
                {
                    Array.Resize(ref frame, (int)Math.Min(length, (long)frame.Length * 2));
                }

                var wanted = (int)Math.Min(length - filled, frame.Length - filled);
                var read = await reader.FillAsync(frame, filled, wanted, cancellationToken);
                filled += read;

                if (reader.OverCap)
                {
                    return null;
                }

                if (read < wanted)
                {
                    return servers; // truncated frame at the end
                }
            }

            try
            {
                servers.Add(Master.Server.Parser.ParseFrom(frame, 0, (int)length));
            }
            catch (InvalidProtocolBufferException)
            {
                // corrupt frame: skip it and keep decoding the rest
            }
        }
    }

    // Counts every body byte and never asks the stream for more than one byte past the
    // cap, so an oversized body is abandoned mid-stream.
    private sealed class CappedReader(Stream source, long maxBytes)
    {
        private long _total;

        public bool OverCap => _total > maxBytes;

        public async Task<int> FillAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var filled = 0;

            while (filled < count && !OverCap)
            {
                // One byte past the cap is enough to prove the body is over it.
                var remaining = maxBytes - _total;
                var allowed = remaining >= count - filled ? count - filled : (int)remaining + 1;
                var read = await source
                    .ReadAsync(buffer.AsMemory(offset + filled, allowed), cancellationToken)
                    .ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }

                filled += read;
                _total += read;
            }

            return filled;
        }
    }
}
