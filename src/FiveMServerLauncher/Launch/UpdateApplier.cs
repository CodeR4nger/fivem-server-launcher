using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
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
            SweepStaleFiles(directory, executableName);

            // The asset downloads straight into the staged file (streamed, never fully in
            // memory).
            using (var content = await httpClient.GetStreamAsync(update.DownloadUrl))
            using (var staged = File.Create(stagedPath))
            {
                await content.CopyToAsync(staged);
            }

            if (!await IsStagedAssetIntactAsync(stagedPath, update))
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

    // Best-effort housekeeping before this update stages anything of its own: backups from
    // previous updates and staged downloads a crashed update left behind are swept away
    // (single-instance, so no live applier owns them). A locked stale file (an old process
    // still running from a backup) never blocks the update.
    private static void SweepStaleFiles(string directory, string executableName)
    {
        foreach (var pattern in new[] { $"{executableName}.old-*", $"{executableName}.new-*" })
        {
            foreach (var stale in Directory.GetFiles(directory, pattern))
            {
                try
                {
                    File.Delete(stale);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    // The staged file must be non-empty and match the published size (a truncated or
    // zero-size download is never installed) and, whenever the release publishes a digest,
    // hash to it (a tampered or corrupted download is never installed either).
    private static async Task<bool> IsStagedAssetIntactAsync(string stagedPath, LauncherUpdate update)
    {
        var stagedLength = new FileInfo(stagedPath).Length;

        if (stagedLength == 0 || stagedLength != update.AssetSize)
        {
            return false;
        }

        if (update.Sha256 is null)
        {
            return true;
        }

        await using var staged = File.OpenRead(stagedPath);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(staged));

        return string.Equals(hash, update.Sha256, StringComparison.OrdinalIgnoreCase);
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
