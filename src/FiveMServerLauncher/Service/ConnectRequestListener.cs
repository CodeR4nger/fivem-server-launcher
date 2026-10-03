using System.IO;
using System.IO.Pipes;
using System.Text;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

public sealed class ConnectRequestListener(string? pipeName = null) : IDisposable
{
    public const string DefaultPipeName = $"Local\\{AppInfo.ProductName}.connect";

    public string PipeName { get; } = pipeName ?? DefaultPipeName;

    private CancellationTokenSource? _linked;

    public void Start(Action<string> onAddress, CancellationToken cancellationToken)
    {
        _linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _linked.Token;
        var pipeName = PipeName;
        _ = Task.Run(() => AcceptLoopAsync(pipeName, onAddress, token));
    }

    private static async Task AcceptLoopAsync(
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
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token);

                using var reader = new StreamReader(server, Encoding.UTF8);
                var line = await reader.ReadLineAsync(token);

                if (line is not null)
                {
                    onAddress(line);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // A faulted connection or a throwing handler must not kill the
                // loop — but must not hot-spin it either.
                await Task.Delay(100, CancellationToken.None);
            }
        }
    }

    public void Dispose()
    {
        _linked?.Cancel();
    }
}
