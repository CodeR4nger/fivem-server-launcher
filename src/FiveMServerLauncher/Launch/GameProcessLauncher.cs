using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

public sealed class GameProcessLauncher(IProcessStarter processStarter, IUriSchemeRegistration uriSchemeRegistration) : IGameProcessLauncher
{
    public Task StartAsync(Uri uri)
    {
        UriShellStarter.Start(processStarter, uriSchemeRegistration, uri);
        return Task.CompletedTask;
    }
}