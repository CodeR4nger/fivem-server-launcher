using System.IO;
using System.Net.Http;

namespace FiveMServerLauncher.Service;

internal static class BoundedContent
{
    public static async Task<byte[]?> ReadAsByteArrayAsync(HttpContent content, long maxBytes)
    {
        if (content.Headers.ContentLength is > 0 and var declared && declared > maxBytes)
        {
            return null;
        }

        using var source = await content.ReadAsStreamAsync();
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];

        while (true)
        {
            var read = await source.ReadAsync(chunk);

            if (read == 0)
            {
                return buffer.ToArray();
            }

            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }
    }
}
