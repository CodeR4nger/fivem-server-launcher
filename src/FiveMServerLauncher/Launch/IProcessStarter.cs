using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

public interface IProcessStarter
{
    void Start(ProcessStartInfo startInfo);
}