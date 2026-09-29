using System.Diagnostics;

namespace FiveMServerLauncher.Service;

// Thin default over the static OS process API. GetProcessesByName allocates one
// Process (with a native handle) per match, and the readiness probe polls
// repeatedly while preparing an external app, so every returned instance must be
// released to keep the probe from leaking handles.
public static class ProcessProbe
{
    public static bool AnyRunning(
        string processName,
        Func<string, Process[]>? fetch = null,
        Action<Process>? release = null)
    {
        var processes = (fetch ?? Fetch)(processName);

        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                (release ?? Dispose)(process);
            }
        }

        // Process.Dispose never calls base Component.Dispose, so the Component
        // Disposed event never fires; the release is an injectable seam with a real
        // default, mirroring the fetch.
        static void Dispose(Process process) => process.Dispose();

        static Process[] Fetch(string name) => Process.GetProcessesByName(name);
    }
}
