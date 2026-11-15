using System.IO;
using System.Net.Http;

namespace FiveMServerLauncher.Service;

/// <summary>
/// The single bounded-download primitive for untrusted bodies (the catalog, server
/// icons). The response is read as it arrives (<see cref="HttpCompletionOption.ResponseHeadersRead"/>),
/// so the cap aborts mid-stream instead of judging a body HttpClient already
/// buffered whole; the response is always disposed, which drops an abandoned
/// connection rather than draining it.
/// </summary>
internal static class BoundedContent
{
    /// <summary>
    /// Sends the request and returns the body, or null when the status is not a
    /// success or the body exceeds <paramref name="maxBytes"/>. Transport failures
    /// propagate to the caller's outage handling: mid-body failures surface as
    /// <see cref="IOException"/>, and the client's <see cref="HttpClient.Timeout"/>
    /// (which, with headers-read, no longer covers the body on its own) bounds the
    /// whole exchange as an <see cref="OperationCanceledException"/>.
    /// </summary>
    public static async Task<byte[]?> GetBytesAsync(HttpClient client, HttpRequestMessage request, long maxBytes)
    {
        using var timeout = new CancellationTokenSource(client.Timeout);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await ReadAsByteArrayAsync(response.Content, maxBytes, timeout.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// The sanctioned outage set for a bounded download: HTTP failures, mid-body
    /// transport failures and timeouts. Anything else is a fault and propagates.
    /// </summary>
    public static bool IsOutage(Exception exception)
    {
        return exception is HttpRequestException or IOException or OperationCanceledException;
    }

    private static async Task<byte[]?> ReadAsByteArrayAsync(HttpContent content, long maxBytes, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > 0 and var declared && declared > maxBytes)
        {
            return null;
        }

        using var source = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];

        while (true)
        {
            var read = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);

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
