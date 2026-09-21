using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

public sealed class ProcessStarter : IProcessStarter
{
    public void Start(ProcessStartInfo startInfo) => Process.Start(startInfo);
}