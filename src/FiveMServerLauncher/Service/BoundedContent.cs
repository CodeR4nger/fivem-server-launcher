using System.IO;
using System.Net;
using System.Net.Http;

namespace FiveMServerLauncher.Service;

/// <summary>
/// The single bounded-download primitive for untrusted bodies (the catalog, server
/// icons). The response is read as it arrives (<see cref="HttpCompletionOption.ResponseHeadersRead"/>),
/// so the cap aborts mid-stream instead of judging a body HttpClient already
/// buffered whole; the response is always disposed, which drops an abandoned
/// connection rather than draining it.
/// </summary>
/// <summary>
/// A bounded download's outcome: the body, or why there is none — a non-success
/// status (possibly transient, carried in <see cref="Status"/> so a caller can tell an
/// authoritative 404 apart) versus a body over the cap (a verdict on the content).
/// </summary>
internal readonly record struct BoundedBody(byte[]? Bytes, bool TooLarge, HttpStatusCode Status = HttpStatusCode.OK)
{
    public static BoundedBody Unsuccessful(HttpStatusCode status) => new(null, TooLarge: false, status);

    public static BoundedBody OverCap { get; } = new(null, TooLarge: true);
}

internal static class BoundedContent
{
    /// <summary>
    /// Sends the request and returns the body, or no body when the status is not a
    /// success or the body exceeds <paramref name="maxBytes"/>. Transport failures
    /// propagate to the caller's outage handling: mid-body failures surface as
    /// <see cref="IOException"/>, and the client's <see cref="HttpClient.Timeout"/>
    /// (which, with headers-read, no longer covers the body on its own) bounds the
    /// whole exchange as an <see cref="OperationCanceledException"/>.
    /// </summary>
    public static async Task<BoundedBody> GetBytesAsync(HttpClient client, HttpRequestMessage request, long maxBytes)
    {
        using var timeout = new CancellationTokenSource(client.Timeout);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return BoundedBody.Unsuccessful(response.StatusCode);
        }

        var bytes = await ReadAsByteArrayAsync(response.Content, maxBytes, timeout.Token).ConfigureAwait(false);

        return bytes is null ? BoundedBody.OverCap : new BoundedBody(bytes, TooLarge: false);
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
