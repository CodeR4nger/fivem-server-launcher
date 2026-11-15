using System.IO;

namespace FiveMServerLauncher.Configuration;

internal enum StoreReadKind
{
    Missing,
    Unreadable,
    Text
}

internal readonly record struct StoreRead(StoreReadKind Kind, string Text = "");

// The read classification both JSON stores share: a missing file is an empty, writable
// store; an unreadable one (AV sharing lock, permission fault) is unknown content that must
// never be overwritten; text is handed back for the store to parse.
internal static class StoreFile
{
    public static StoreRead Read(string path, Func<string, string> readAllText)
    {
        if (!File.Exists(path))
        {
            return new StoreRead(StoreReadKind.Missing);
        }

        try
        {
            return new StoreRead(StoreReadKind.Text, readAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new StoreRead(StoreReadKind.Unreadable);
        }
    }

    public static string BackupPath(string path) => path + ".bak";

    // Copies a corrupt store beside itself before anything can replace it. False means the
    // corrupt file is still the only copy, so the caller must treat the store as unreadable.
    public static bool TryBackUpCorrupt(string path)
    {
        try
        {
            File.Copy(path, BackupPath(path), overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
