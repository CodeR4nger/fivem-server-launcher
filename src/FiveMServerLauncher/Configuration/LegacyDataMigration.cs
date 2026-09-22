using System.IO;

namespace FiveMServerLauncher.Configuration;

public static class LegacyDataMigration
{
    public static void Migrate(
        string legacyDirectory,
        IEnumerable<string> portableFiles,
        Action<string, string>? copyFile = null)
    {
        ArgumentNullException.ThrowIfNull(legacyDirectory);
        ArgumentNullException.ThrowIfNull(portableFiles);

        var copy = copyFile ?? File.Copy;

        foreach (var portableFile in portableFiles)
        {
            var legacyFile = Path.Combine(legacyDirectory, Path.GetFileName(portableFile));

            if (File.Exists(legacyFile) && !File.Exists(portableFile))
            {
                try
                {
                    copy(legacyFile, portableFile);
                }
                catch (IOException)
                {
                    // Best-effort: locked/unwritable files are skipped, never crash startup.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort: files that cannot be copied (read-only target) are skipped.
                }
            }
        }
    }
}