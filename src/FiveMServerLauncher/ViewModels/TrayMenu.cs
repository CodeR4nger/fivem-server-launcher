using FiveMServerLauncher.Localization;

namespace FiveMServerLauncher.ViewModels;

public enum TrayMenuEntryKind
{
    Server,
    ShowAll,
    NoServers,
    Separator,
    Show,
    Exit
}

/// <summary>
/// One line of the notification-area menu. <see cref="Address"/> is set only for
/// <see cref="TrayMenuEntryKind.Server"/>; <see cref="Label"/> is plain text (the native
/// glue escapes the Win32 mnemonic prefix).
/// </summary>
public sealed record TrayMenuEntry(TrayMenuEntryKind Kind, string Label, string? Address, bool IsEnabled);

public static class TrayMenu
{
    public const int MaxServers = 15;
    public const int MaxLabelLength = 60;

    public static IReadOnlyList<TrayMenuEntry> Build(
        IReadOnlyList<SavedServerItem> servers,
        bool isBusy,
        ILocalizer localizer)
    {
        var entries = new List<TrayMenuEntry>();

        // Busy servers are greyed out; the click path's busy guard still ignores a
        // click from a menu that was opened before the connect started.
        entries.AddRange(servers.Take(MaxServers).Select(server =>
            new TrayMenuEntry(TrayMenuEntryKind.Server, Shorten(server.Name), server.Address, !isBusy)));

        if (servers.Count > MaxServers)
        {
            entries.Add(new TrayMenuEntry(TrayMenuEntryKind.ShowAll, localizer.Get("TrayAllServers"), null, true));
        }
        else if (servers.Count == 0)
        {
            entries.Add(new TrayMenuEntry(TrayMenuEntryKind.NoServers, localizer.Get("TrayNoServers"), null, false));
        }

        entries.Add(new TrayMenuEntry(TrayMenuEntryKind.Separator, string.Empty, null, true));
        entries.Add(new TrayMenuEntry(TrayMenuEntryKind.Show, localizer.Get("TrayShow"), null, true));
        entries.Add(new TrayMenuEntry(TrayMenuEntryKind.Exit, localizer.Get("TrayExit"), null, true));
        return entries;
    }

    // Server names have no length limit; a menu wider than the screen is useless. The cut
    // never leaves a lone high surrogate (half an emoji) before the ellipsis.
    private static string Shorten(string name)
    {
        if (name.Length <= MaxLabelLength)
        {
            return name;
        }

        var keep = MaxLabelLength - 1;

        if (char.IsHighSurrogate(name[keep - 1]))
        {
            keep--;
        }

        return string.Concat(name.AsSpan(0, keep), "…");
    }
}
