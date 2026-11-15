using FiveMServerLauncher.Core;
using Microsoft.Win32;

namespace FiveMServerLauncher.Launch;

public sealed class UriSchemeRegistration : IUriSchemeRegistration
{
    private const string UserClasses = @"Software\Classes\";

    public bool IsSchemeRegistered(string scheme)
    {
        // The opened key holds a native registry handle until disposed.
        using var key = Registry.ClassesRoot.OpenSubKey(scheme);
        return key is not null;
    }

    public string? GetUserOpenCommand(string scheme)
    {
        using var key = Registry.CurrentUser.OpenSubKey(UserClasses + scheme + @"\shell\open\command");
        return key?.GetValue(null) as string;
    }

    public void RegisterForUser(string scheme, string openCommand, string iconPath)
    {
        using var root = Registry.CurrentUser.CreateSubKey(UserClasses + scheme);
        root.SetValue(null, "URL:" + AppInfo.ProductName);
        root.SetValue("URL Protocol", string.Empty);

        using var icon = root.CreateSubKey("DefaultIcon");
        icon.SetValue(null, iconPath);

        using var command = root.CreateSubKey(@"shell\open\command");
        command.SetValue(null, openCommand);
    }

    public void UnregisterForUser(string scheme)
    {
        Registry.CurrentUser.DeleteSubKeyTree(UserClasses + scheme, throwOnMissingSubKey: false);
    }
}
