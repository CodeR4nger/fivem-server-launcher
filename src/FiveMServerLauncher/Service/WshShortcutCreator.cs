namespace FiveMServerLauncher.Service;

public sealed class WshShortcutCreator : IShortcutCreator
{
    public void CreateShortcut(string shortcutPath, string targetPath, string arguments, string? iconPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is not registered");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.Arguments = arguments;

        if (iconPath is not null)
        {
            shortcut.IconLocation = iconPath;
        }

        shortcut.Save();
    }
}
