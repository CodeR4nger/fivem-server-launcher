using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

public sealed class GameProcessLauncher : IGameProcessLauncher
{
    public Task StartAsync(Uri uri)
    {
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        return Task.CompletedTask;
    }
}