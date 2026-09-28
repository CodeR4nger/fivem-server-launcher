using System.IO;
using FiveMServerLauncher.Core;
using FiveMServerLauncher.Tests.Configuration;

namespace FiveMServerLauncher.Tests;

public class AtomicFileTests
{
    [Fact]
    public void WriteAllText_WhenTargetExists_ShouldReplaceContentAndLeaveNoTempFiles()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var path = tempDir.FilePath;
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(path, "old content");

        // When
        AtomicFile.WriteAllText(path, "new content");

        // Then
        Assert.Equal("new content", File.ReadAllText(path));
        Assert.Equal(path, Assert.Single(Directory.GetFiles(tempDir.DirectoryPath)));
    }

    [Fact]
    public void WriteAllText_WhenTargetMissing_ShouldCreateFileWithoutTempResidue()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var path = tempDir.FilePath;

        // When
        AtomicFile.WriteAllText(path, "content");

        // Then
        Assert.True(File.Exists(path));
        Assert.Equal("content", File.ReadAllText(path));
        Assert.Equal(path, Assert.Single(Directory.GetFiles(tempDir.DirectoryPath)));
    }

    [Fact]
    public void WriteAllText_WhenDirectoryMissing_ShouldCreateDirectory()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var path = Path.Combine(tempDir.DirectoryPath, "nested", "data.json");

        // When
        AtomicFile.WriteAllText(path, "content");

        // Then
        Assert.True(File.Exists(path));
        Assert.Equal("content", File.ReadAllText(path));
    }

    [Fact]
    public void WriteAllText_WhenStaleTempFileExists_ShouldNotAffectTheTarget()
    {
        // Given — a leftover temp file from a previous crash must not break the next write.
        using var tempDir = new TempSettingsDirectory();
        var path = tempDir.FilePath;
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(path, "old");

        // When
        AtomicFile.WriteAllText(path, "new");

        // Then
        Assert.Equal("new", File.ReadAllText(path));
    }
}
