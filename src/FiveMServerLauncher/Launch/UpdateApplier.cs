using System.Diagnostics;
using System.IO;
using System.Net.Http;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Launch;

public interface IUpdateApplier
{
    Task<bool> ApplyAsync(LauncherUpdate update);
}

public sealed class UpdateApplier(
    HttpClient httpClient,
    Func<string?> executablePathProvider,
    IProcessStarter processStarter,
    IDisposable singleInstanceGuard,
    Action exitApplication) : IUpdateApplier
{
    public async Task<bool> ApplyAsync(LauncherUpdate update)
    {
        var executablePath = executablePathProvider();

        if (executablePath is null)
        {
            return false;
        }

        var executableName = Path.GetFileName(executablePath);

        // Sanity: only an asset named like the running exe's own file is a true in-place
        // replacement; anything else is refused before a single byte is downloaded. File
        // names are case-insensitive on Windows, so the comparison follows the OS.
        if (!string.Equals(update.AssetName, executableName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(executablePath)!;
        var suffix = Guid.NewGuid().ToString("N");
        var backupPath = Path.Combine(directory, $"{executableName}.old-{suffix}");
        var stagedPath = Path.Combine(directory, $"{executableName}.new-{suffix}");

        try
        {
            // Best-effort housekeeping: backups from previous updates are swept away. A
            // locked stale backup (its old process still running) never blocks the update.
            foreach (var stale in Directory.GetFiles(directory, $"{executableName}.old-*"))
            {
                try
                {
                    File.Delete(stale);
                }
                catch (Exception)
                {
                }
            }

            // The asset downloads straight into the staged file (streamed, never fully in
            // memory).
            using (var content = await httpClient.GetStreamAsync(update.DownloadUrl))
            using (var staged = File.Create(stagedPath))
            {
                await content.CopyToAsync(staged);
            }

            // Sanity: the staged file must be non-empty and match the published size (a
            // truncated or zero-size download is never installed).
            var stagedLength = new FileInfo(stagedPath).Length;

            if (stagedLength == 0 || stagedLength != update.AssetSize)
            {
                Rollback(executablePath, backupPath, stagedPath);
                return false;
            }
            // Swapping a running exe is allowed on Windows: rename it out of the way, then
            // move the staged download into its place.
            File.Move(executablePath, backupPath);
            File.Move(stagedPath, executablePath);

            // The relaunched instance must win the single-instance mutex: the lock is
            // released before the new process can start acquiring it.
            singleInstanceGuard.Dispose();

            processStarter.Start(new ProcessStartInfo(executablePath)
            {
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            Rollback(executablePath, backupPath, stagedPath);
            return false;
        }

        // Only a completed handoff reaches the exit; a throwing exit can never roll back an
        // already-running new instance.
        exitApplication();

        return true;
    }

    private static void Rollback(string executablePath, string backupPath, string stagedPath)
    {
        // Best-effort: whatever step failed, the running exe — possibly already renamed to
        // the backup — must end up back in place.
        try
        {
            if (File.Exists(backupPath))
            {
                if (File.Exists(executablePath))
                {
                    // Atomically put the backup back in place: no window without an exe.
                    File.Replace(backupPath, executablePath, null);
                }
                else
                {
                    File.Move(backupPath, executablePath);
                }
            }

            if (File.Exists(stagedPath))
            {
                File.Delete(stagedPath);
            }
        }
        catch (Exception)
        {
            // Rollback is best-effort; the failure is reported to the caller either way.
        }
    }
}
