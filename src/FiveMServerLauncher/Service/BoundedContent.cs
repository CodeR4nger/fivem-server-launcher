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
    public static Task<BoundedBody> GetBytesAsync(HttpClient client, HttpRequestMessage request, long maxBytes)
    {
        return SendAsync(
            client,
            request,
            maxBytes,
            async (body, token) => await ReadAsByteArrayAsync(body, maxBytes, token).ConfigureAwait(false) is { } bytes
                ? new BoundedBody(bytes, TooLarge: false)
                : BoundedBody.OverCap,
            BoundedBody.Unsuccessful,
            BoundedBody.OverCap);
    }

    /// <summary>
    /// The streaming twin of <see cref="GetBytesAsync"/> for a body the caller consumes as
    /// it arrives (the catalog decodes frame by frame): same headers-read, timeout and
    /// dispose rules; null when the status is not a success or the declared length is over
    /// <paramref name="maxBytes"/>. The reader owns the cap while it reads.
    /// </summary>
    public static Task<T?> ReadStreamAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        long maxBytes,
        Func<Stream, CancellationToken, Task<T?>> read)
        where T : class
    {
        return SendAsync(client, request, maxBytes, read, _ => null, null);
    }

    private static async Task<T> SendAsync<T>(
        HttpClient client,
        HttpRequestMessage request,
        long maxBytes,
        Func<Stream, CancellationToken, Task<T>> read,
        Func<HttpStatusCode, T> unsuccessful,
        T overCap)
    {
        using var timeout = new CancellationTokenSource(client.Timeout);
        using var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return unsuccessful(response.StatusCode);
        }

        if (response.Content.Headers.ContentLength is > 0 and var declared && declared > maxBytes)
        {
            return overCap;
        }

        using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);

        return await read(body, timeout.Token).ConfigureAwait(false);
    }

    /// <summary>
    /// The sanctioned outage set for a bounded download: HTTP failures, mid-body
    /// transport failures and timeouts. Anything else is a fault and propagates.
    /// </summary>
    public static bool IsOutage(Exception exception)
    {
        return exception is HttpRequestException or IOException or OperationCanceledException;
    }

    private static async Task<byte[]?> ReadAsByteArrayAsync(Stream source, long maxBytes, CancellationToken cancellationToken)
    {
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
