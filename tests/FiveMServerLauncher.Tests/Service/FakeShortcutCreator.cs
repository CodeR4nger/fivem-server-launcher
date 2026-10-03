using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

internal sealed record ShortcutCall(string ShortcutPath, string TargetPath, string Arguments, string? IconPath);

internal sealed class FakeShortcutCreator : IShortcutCreator
{
    public List<ShortcutCall> Calls { get; } = new();

    public bool ThrowOnCreate { get; set; }

    public void CreateShortcut(string shortcutPath, string targetPath, string arguments, string? iconPath)
    {
        if (ThrowOnCreate)
        {
            throw new InvalidOperationException("shell broken");
        }

        Calls.Add(new ShortcutCall(shortcutPath, targetPath, arguments, iconPath));
    }
}
