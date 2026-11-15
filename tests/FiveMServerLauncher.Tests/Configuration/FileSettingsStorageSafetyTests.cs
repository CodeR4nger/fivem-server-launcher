using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class FileSettingsStorageSafetyTests
{
    private const string StoredSettings = """{"PreferredClient":"RedM","AutoLaunch":true,"LastServerAddress":"abc123"}""";

    private static TempSettingsDirectory WithFile(string content)
    {
        var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, content);
        return tempDir;
    }

    private static LauncherSettings Changed() => new() { AutoLaunch = false, PreferredClient = GameClient.FiveM };

    [Fact]
    public void Save_AfterLoadFailedWithIOException_ShouldThrowAndLeaveFileByteIdentical()
    {
        // Given — a locked file read as "defaults" must never be overwritten with them.
        using var tempDir = WithFile(StoredSettings);
        var before = File.ReadAllBytes(tempDir.FilePath);
        var storage = new FileSettingsStorage(tempDir.FilePath, _ => throw new IOException("locked"));
        Assert.Null(storage.Load());

        // When / Then
        Assert.Throws<StoreUnavailableException>(() => storage.Save(Changed()));
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
    }

    [Fact]
    public void Save_AfterLoadHitARealLock_ShouldThrowAndLeaveFileByteIdentical()
    {
        // Given
        using var tempDir = WithFile(StoredSettings);
        var before = File.ReadAllBytes(tempDir.FilePath);
        var storage = new FileSettingsStorage(tempDir.FilePath);

        using (File.Open(tempDir.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Null(storage.Load());
        }

        // When / Then — the lock is gone, but the caller's settings were built from defaults.
        Assert.Throws<StoreUnavailableException>(() => storage.Save(Changed()));
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
    }

    [Fact]
    public void Load_WhenReadIsDenied_ShouldReturnNullAndBlockSave()
    {
        // Given
        using var tempDir = WithFile(StoredSettings);
        var storage = new FileSettingsStorage(tempDir.FilePath, _ => throw new UnauthorizedAccessException());

        // When / Then
        Assert.Null(storage.Load());
        Assert.Throws<StoreUnavailableException>(() => storage.Save(Changed()));
    }

    [Fact]
    public void Save_AfterALaterLoadSucceeds_ShouldWrite()
    {
        // Given
        using var tempDir = WithFile(StoredSettings);
        var calls = 0;
        var storage = new FileSettingsStorage(
            tempDir.FilePath,
            path => ++calls == 1 ? throw new IOException("locked") : File.ReadAllText(path));
        Assert.Null(storage.Load());
        Assert.NotNull(storage.Load());

        // When
        storage.Save(Changed());

        // Then
        Assert.False(new FileSettingsStorage(tempDir.FilePath).Load()!.AutoLaunch);
    }

    [Theory]
    [InlineData("{ invalid json")]
    [InlineData("[1, 2]")]
    [InlineData("null")]
    [InlineData("")]
    public void Load_WhenFileIsCorrupt_ShouldKeepAByteIdenticalBackup(string content)
    {
        // Given
        using var tempDir = WithFile(content);
        var before = File.ReadAllBytes(tempDir.FilePath);
        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        Assert.Null(storage.Load());
        storage.Save(Changed());

        // Then — the corrupt original survives beside the new file.
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath + ".bak"));
        Assert.Equal(GameClient.FiveM, new FileSettingsStorage(tempDir.FilePath).Load()!.PreferredClient);
    }

    [Fact]
    public void Save_AfterCorruptLoadWhoseBackupFailed_ShouldThrowAndLeaveFileByteIdentical()
    {
        // Given — the .bak path is a directory, so the corrupt file stays the only copy.
        using var tempDir = WithFile("{ invalid json");
        Directory.CreateDirectory(tempDir.FilePath + ".bak");
        var before = File.ReadAllBytes(tempDir.FilePath);
        var storage = new FileSettingsStorage(tempDir.FilePath);
        Assert.Null(storage.Load());

        // When / Then
        Assert.Throws<StoreUnavailableException>(() => storage.Save(Changed()));
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
    }
}
