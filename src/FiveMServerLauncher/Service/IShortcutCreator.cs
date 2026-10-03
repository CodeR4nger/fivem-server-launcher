namespace FiveMServerLauncher.Service;

public interface IShortcutCreator
{
    void CreateShortcut(string shortcutPath, string targetPath, string arguments, string? iconPath);
}
