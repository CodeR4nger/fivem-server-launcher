using System.ComponentModel;
using System.Diagnostics;

namespace FiveMServerLauncher.Launch;

internal static class UriShellStarter
{
    public static void Start(IProcessStarter processStarter, IUriSchemeRegistration uriSchemeRegistration, Uri uri)
    {
        if (!uriSchemeRegistration.IsSchemeRegistered(uri.Scheme))
        {
            throw new Win32Exception("No application is associated with the specified file.");
        }

        processStarter.Start(new ProcessStartInfo("explorer.exe", uri.AbsoluteUri));
    }
}