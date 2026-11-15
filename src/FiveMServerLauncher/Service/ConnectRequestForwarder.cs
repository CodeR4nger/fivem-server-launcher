using System.IO.Pipes;
using System.Text;

namespace FiveMServerLauncher.Service;

public sealed class ConnectRequestForwarder(string? pipeName = null)
{
    public string PipeName { get; } = pipeName ?? ConnectPipeName.ForCurrentSession();

    public bool TryForward(string address, int timeoutMs = 2000)
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".", PipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            client.Connect(timeoutMs);

            var bytes = Encoding.UTF8.GetBytes(address + "\n");
            client.Write(bytes);
            client.Flush();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
