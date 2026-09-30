using System.Diagnostics;
using System.Net;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.Launch;

public class UpdateApplierTests
{
    private static LauncherUpdate Update(long assetSize) =>
        new("v1.4.0", "CFXLauncher.exe", assetSize, "https://example.invalid/CFXLauncher.exe");

    private static UpdateApplier CreateApplier(
        HttpMessageHandler handler,
        Func<string?> executablePathProvider,
        List<string> order,
        IProcessStarter? processStarter = null)
    {
        return new UpdateApplier(
            new HttpClient(handler),
            executablePathProvider,
            processStarter ?? new RecordingProcessStarter(order),
            new RecordingGuard(order),
            () => order.Add("exit"));
    }

    private sealed class TempLauncherDirectory : IDisposable
    {
        public TempLauncherDirectory()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(DirectoryPath);
            ExePath = Path.Combine(DirectoryPath, "CFXLauncher.exe");
            File.WriteAllText(ExePath, "old-launcher");
        }

        public string DirectoryPath { get; }

        public string ExePath { get; }

        public void Dispose()
        {
            Directory.Delete(DirectoryPath, true);
        }
    }

    private sealed class RecordingProcessStarter(List<string> order) : IProcessStarter
    {
        public void Start(ProcessStartInfo startInfo) => order.Add($"relaunch:{startInfo.FileName}");
    }

    private sealed class RecordingGuard(List<string> order) : IDisposable
    {
        public void Dispose() => order.Add("guard-released");
    }

    private sealed class ThrowingProcessStarter : IProcessStarter
    {
        public void Start(ProcessStartInfo startInfo)
        {
            throw new InvalidOperationException("Simulated spawn failure");
        }
    }

    [Fact]
    public async Task ApplyAsync_WhenDownloadSucceeds_ShouldSwapRelaunchAndExit()
    {
        // Given
        using var directory = new TempLauncherDirectory();
        var order = new List<string>();
        byte[] asset = [1, 2, 3, 4, 5];
        var applier = CreateApplier(
            new FakeHttpMessageHandler(HttpStatusCode.OK, asset),
            () => directory.ExePath,
            order);

        // When
        var applied = await applier.ApplyAsync(Update(asset.Length));

        // Then: the update is handed off and the downloaded bytes are the exe now
        Assert.True(applied);
        Assert.Equal(asset, File.ReadAllBytes(directory.ExePath));

        // And: the replaced exe is kept as a backup beside it
        var backups = Directory.GetFiles(directory.DirectoryPath, "CFXLauncher.exe.old-*");
        Assert.Single(backups);
        Assert.Equal("old-launcher", File.ReadAllText(backups[0]));

        // And: the handoff order is lock released, then relaunch of the new exe, then exit
        Assert.Equal(["guard-released", $"relaunch:{directory.ExePath}", "exit"], order);

        // And: no staged file is left behind
        Assert.Empty(Directory.GetFiles(directory.DirectoryPath, "CFXLauncher.exe.new-*"));
    }

    [Fact]
    public async Task ApplyAsync_WhenDownloadFails_ShouldLeaveExeUntouchedAndReportFailure()
    {
        // Given
        using var directory = new TempLauncherDirectory();
        var order = new List<string>();
        var applier = CreateApplier(
            new FakeHttpMessageHandler(true),
            () => directory.ExePath,
            order);

        // When
        var applied = await applier.ApplyAsync(Update(5));

        // Then: failure, the running exe is untouched and nothing was handed off
        Assert.False(applied);
        Assert.Equal("old-launcher", File.ReadAllText(directory.ExePath));
        Assert.Empty(order);

        // And: no staged or backup files are left behind
        Assert.Empty(Directory.GetFiles(directory.DirectoryPath, "CFXLauncher.exe.*-*"));
    }

    [Fact]
    public async Task ApplyAsync_WhenStagedBytesDoNotMatchPublishedSize_ShouldReportFailure()
    {
        // Given: the download arrives truncated relative to the published asset size
        using var directory = new TempLauncherDirectory();
        var order = new List<string>();
        byte[] truncated = [1, 2, 3];
        var applier = CreateApplier(
            new FakeHttpMessageHandler(HttpStatusCode.OK, truncated),
            () => directory.ExePath,
            order);

        // When
        var applied = await applier.ApplyAsync(Update(truncated.Length + 10));

        // Then: no swap, the exe is untouched, nothing was handed off or left behind
        Assert.False(applied);
        Assert.Equal("old-launcher", File.ReadAllText(directory.ExePath));
        Assert.Empty(order);
        Assert.Empty(Directory.GetFiles(directory.DirectoryPath, "CFXLauncher.exe.*-*"));
    }

    [Fact]
    public async Task ApplyAsync_WhenAssetNameDoesNotMatchRunningExe_ShouldRefuseWithoutDownloading()
    {
        // Given: the update carries an asset named unlike the running exe's own file
        using var directory = new TempLauncherDirectory();
        var order = new List<string>();
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, [1, 2, 3]);
        var applier = CreateApplier(handler, () => directory.ExePath, order);

        // When
        var applied = await applier.ApplyAsync(
            new LauncherUpdate("v1.4.0", "CFXLauncher.zip", 3, "https://example.invalid/CFXLauncher.zip"));

        // Then: refused before any download or file change
        Assert.False(applied);
        Assert.Equal(0, handler.RequestCount);
        Assert.Equal("old-launcher", File.ReadAllText(directory.ExePath));
        Assert.Empty(order);
    }

    [Fact]
    public async Task ApplyAsync_WhenExecutablePathUnresolvable_ShouldReportFailure()
    {
        // Given
        var order = new List<string>();
        var applier = CreateApplier(new FakeHttpMessageHandler(true), () => null, order);

        // When
        var applied = await applier.ApplyAsync(Update(3));

        // Then
        Assert.False(applied);
        Assert.Empty(order);
    }

    [Fact]
    public async Task ApplyAsync_WhenRelaunchFails_ShouldRestoreRunningExeAndReportFailure()
    {
        // Given
        using var directory = new TempLauncherDirectory();
        var order = new List<string>();
        byte[] asset = [9, 9, 9];
        var applier = CreateApplier(
            new FakeHttpMessageHandler(HttpStatusCode.OK, asset),
            () => directory.ExePath,
            order,
            new ThrowingProcessStarter());

        // When
        var applied = await applier.ApplyAsync(Update(asset.Length));

        // Then: failure with the old exe back in place (no broken install)
        Assert.False(applied);
        Assert.Equal("old-launcher", File.ReadAllText(directory.ExePath));

        // And: no swapped-out or staged files remain
        Assert.Empty(Directory.GetFiles(directory.DirectoryPath, "CFXLauncher.exe.*-*"));

        // And: no exit — the launcher keeps running. The lock was already handed off before
        // the relaunch attempt (the accepted handoff edge: it stays released until restart).
        Assert.Equal(["guard-released"], order);
    }

    [Fact]
    public async Task ApplyAsync_WhenStaleBackupsExist_ShouldSweepThemOnSuccess()
    {
        // Given: a previous update left a backup behind
        using var directory = new TempLauncherDirectory();
        var staleBackup = Path.Combine(directory.DirectoryPath, "CFXLauncher.exe.old-deadbeef");
        File.WriteAllText(staleBackup, "ancient");
        var order = new List<string>();
        byte[] asset = [1, 2, 3];
        var applier = CreateApplier(
            new FakeHttpMessageHandler(HttpStatusCode.OK, asset),
            () => directory.ExePath,
            order);

        // When
        var applied = await applier.ApplyAsync(Update(asset.Length));

        // Then: applied, and only this update's own backup remains
        Assert.True(applied);
        Assert.False(File.Exists(staleBackup));
        var backups = Directory.GetFiles(directory.DirectoryPath, "CFXLauncher.exe.old-*");
        Assert.Single(backups);
        Assert.Equal("old-launcher", File.ReadAllText(backups[0]));
    }

    [Fact]
    public async Task ApplyAsync_WhenAssetNameDiffersOnlyByCase_ShouldApply()
    {
        // Given: Windows file names are case-insensitive, so a differently-cased asset is
        // still a true in-place replacement
        using var directory = new TempLauncherDirectory();
        var order = new List<string>();
        byte[] asset = [7, 8, 9];
        var applier = CreateApplier(
            new FakeHttpMessageHandler(HttpStatusCode.OK, asset),
            () => directory.ExePath,
            order);

        // When
        var applied = await applier.ApplyAsync(
            new LauncherUpdate("v1.4.0", "cfxlauncher.exe", asset.Length, "https://example.invalid/CFXLauncher.exe"));

        // Then
        Assert.True(applied);
        Assert.Equal(asset, File.ReadAllBytes(directory.ExePath));
    }

    [Fact]
    public async Task ApplyAsync_WhenRenameStepFails_ShouldLeaveExeInPlaceAndReportFailure()
    {
        // Given: another process holds the exe open without sharing delete — the rename
        // step of the swap fails
        using var directory = new TempLauncherDirectory();
        var order = new List<string>();
        byte[] asset = [1, 2, 3];
        using var exeLock = File.Open(directory.ExePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var applier = CreateApplier(
            new FakeHttpMessageHandler(HttpStatusCode.OK, asset),
            () => directory.ExePath,
            order);

        // When
        var applied = await applier.ApplyAsync(Update(asset.Length));

        // Then: failure, no partial swap, nothing handed off
        Assert.False(applied);
        Assert.Empty(order);

        // And: the running exe is untouched (still readable under the lock) and no
        // staged or backup files remain
        using var reader = new StreamReader(exeLock);
        exeLock.Position = 0;
        Assert.Equal("old-launcher", reader.ReadToEnd());
        Assert.Empty(Directory.GetFiles(directory.DirectoryPath, "CFXLauncher.exe.*-*"));
    }
}
