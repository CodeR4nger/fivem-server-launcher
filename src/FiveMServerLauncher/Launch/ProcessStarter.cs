using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

public sealed class ProcessStarter : IProcessStarter
{
    public void Start(ProcessStartInfo startInfo)
    {
        // The returned wrapper holds a native process handle; disposing it does NOT
        // kill the started process (we never manage its lifetime).
        using var process = Process.Start(startInfo);
    }
}
