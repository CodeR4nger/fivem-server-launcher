using System.ComponentModel;
using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

public sealed class GameProcessLauncher(IProcessStarter processStarter, IUriSchemeRegistration uriSchemeRegistration) : IGameProcessLauncher
{
    public Task StartAsync(Uri uri)
    {
        if (!uriSchemeRegistration.IsSchemeRegistered(uri.Scheme))
        {
            throw new Win32Exception("No application is associated with the specified file.");
        }

        processStarter.Start(new ProcessStartInfo("explorer.exe", uri.AbsoluteUri));
        return Task.CompletedTask;
    }
}