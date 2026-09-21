using System.Diagnostics;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Launch;

public interface IExternalAppStarter
{
    Task StartAsync(ExternalApp app);
}

public sealed class ExternalAppStarter(IProcessStarter processStarter, IUriSchemeRegistration uriSchemeRegistration) : IExternalAppStarter
{
    public Task StartAsync(ExternalApp app)
    {
        var uri = UriOf(app);
        if (uri is null)
        {
            return Task.CompletedTask;
        }

        UriShellStarter.Start(processStarter, uriSchemeRegistration, uri);
        return Task.CompletedTask;
    }

    private static Uri? UriOf(ExternalApp app)
    {
        return app switch
        {
            ExternalApp.Steam => new Uri("steam://"),
            ExternalApp.Discord => new Uri("discord://"),
            _ => null
        };
    }
}