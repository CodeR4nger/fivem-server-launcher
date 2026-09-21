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

    public Task StartExecutableAsync(string executablePath, IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return StartExecutableAsync(executablePath);
        }

        processStarter.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = string.Join(' ', arguments),
            UseShellExecute = true,
        });
        return Task.CompletedTask;
    }
}