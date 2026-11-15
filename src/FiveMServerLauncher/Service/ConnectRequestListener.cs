using System.IO;
using System.IO.Pipes;
using System.Text;

namespace FiveMServerLauncher.Service;

public sealed class ConnectRequestListener(
    string? pipeName = null,
    TimeSpan? readTimeout = null,
    int maxLineBytes = ConnectRequestListener.DefaultMaxLineBytes) : IDisposable
{
    // The forwarder writes right after connecting, and the longest valid address is far
    // shorter than the cap: a slower or longer client is not a forwarder.
    public const int DefaultMaxLineBytes = 2048;

    public static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(2);

    public string PipeName { get; } = pipeName ?? ConnectPipeName.ForCurrentSession();

    private readonly TimeSpan _readTimeout = readTimeout ?? DefaultReadTimeout;

    private CancellationTokenSource? _linked;

    /// <summary>
    /// The accept loop: completes once the loop has exited after cancellation (the server
    /// end is disposed by then). Completed when the listener was never started.
    /// </summary>
    public Task Completion { get; private set; } = Task.CompletedTask;

    public void Start(Action<string> onAddress, CancellationToken cancellationToken)
    {
        _linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _linked.Token;
        var pipeName = PipeName;
        Completion = Task.Run(() => AcceptLoopAsync(pipeName, onAddress, token));
    }

    private async Task AcceptLoopAsync(
        string pipeName,
        Action<string> onAddress,
        CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await server.WaitForConnectionAsync(token);

                var line = await ReadBoundedLineAsync(server, token);

                if (line is not null)
                {
                    onAddress(line);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // A faulted, silent or oversized connection or a throwing handler must not
                // kill the loop — but must not hot-spin it either.
                await Task.Delay(100, CancellationToken.None);
            }
        }
    }

    // ReadLine semantics (first line, "\r\n" or "\n", a final unterminated line counts, EOF
    // with no data is null) under two bounds: the whole read finishes within the timeout and
    // never buffers more than the cap. Breaking either bound throws — the connection drops
    // without dispatch and the loop accepts the next client.
    private async Task<string?> ReadBoundedLineAsync(Stream stream, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(_readTimeout);
        var buffer = new byte[maxLineBytes + 1];
        var length = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(length), timeout.Token);

            if (read == 0)
            {
                return length == 0 ? null : Decode(buffer, length);
            }

            var newline = Array.IndexOf(buffer, (byte)'\n', length, read);
            length += read;

            if (newline >= 0)
            {
                return Decode(buffer, newline);
            }

            if (length > maxLineBytes)
            {
                throw new InvalidDataException("Connect request exceeds the line cap.");
            }
        }
    }

    private static string Decode(byte[] buffer, int length)
    {
        if (length > 0 && buffer[length - 1] == '\r')
        {
            length--;
        }

        return Encoding.UTF8.GetString(buffer, 0, length);
    }

    public void Dispose()
    {
        _linked?.Cancel();
    }
}
