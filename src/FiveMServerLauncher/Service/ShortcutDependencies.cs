namespace FiveMServerLauncher.Service;

public sealed record ShortcutDependencies(
    IShortcutCreator Creator,
    Func<string> DesktopPathProvider,
    Func<string?> TargetExePathProvider,
    ShortcutIconWriter? IconWriter = null);
