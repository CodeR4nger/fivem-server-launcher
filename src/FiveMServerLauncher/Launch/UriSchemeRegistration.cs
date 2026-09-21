using Microsoft.Win32;

namespace FiveMServerLauncher.Launch;

public sealed class UriSchemeRegistration : IUriSchemeRegistration
{
    public bool IsSchemeRegistered(string scheme) => Registry.ClassesRoot.OpenSubKey(scheme) is not null;
}