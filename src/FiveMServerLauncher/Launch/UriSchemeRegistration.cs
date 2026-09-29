using Microsoft.Win32;

namespace FiveMServerLauncher.Launch;

public sealed class UriSchemeRegistration : IUriSchemeRegistration
{
    public bool IsSchemeRegistered(string scheme)
    {
        // The opened key holds a native registry handle until disposed.
        using var key = Registry.ClassesRoot.OpenSubKey(scheme);
        return key is not null;
    }
}
