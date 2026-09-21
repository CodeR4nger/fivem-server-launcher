using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

public sealed class GameProcessLauncher(IProcessStarter processStarter, IUriSchemeRegistration uriSchemeRegistration) : IGameProcessLauncher
{
    public Task StartAsync(Uri uri)
    {
        UriShellStarter.Start(processStarter, uriSchemeRegistration, uri);
        return Task.CompletedTask;
    }

    public Task StartExecutableAsync(string executablePath)
    {
        processStarter.Start(new ProcessStartInfo("explorer.exe", $"\"{executablePath}\""));
        return Task.CompletedTask;
    }
}