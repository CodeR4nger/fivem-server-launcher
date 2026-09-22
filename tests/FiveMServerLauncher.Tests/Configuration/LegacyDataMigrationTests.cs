using FiveMServerLauncher.Configuration;

namespace FiveMServerLauncher.Tests.Configuration;

public class LegacyDataMigrationTests : IDisposable
{
    private readonly string _legacyDir;
    private readonly string _portableDir;

    public LegacyDataMigrationTests()
    {
        _legacyDir = Path.Combine(Path.GetTempPath(), $"cmx-legacy-{Guid.NewGuid():N}");
        _portableDir = Path.Combine(Path.GetTempPath(), $"cmx-portable-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_legacyDir);
        Directory.CreateDirectory(_portableDir);
    }

    public void Dispose()
    {
        Directory.Delete(_legacyDir, recursive: true);
        Directory.Delete(_portableDir, recursive: true);
    }

    [Fact]
    public void Migrate_WhenLegacyExistsAndPortableEmpty_ShouldCopyLegacyFiles()
    {
        // Given only legacy files exist, When migrating, Then they are copied into the portable dir.
        File.WriteAllText(Path.Combine(_legacyDir, "launcher-settings.json"), "{legacy-settings}");
        File.WriteAllText(Path.Combine(_legacyDir, "saved-servers.json"), "[legacy-servers]");
        LegacyDataMigration.Migrate(_legacyDir, PortableFiles());

        Assert.Equal("{legacy-settings}", File.ReadAllText(Path.Combine(_portableDir, "launcher-settings.json")));
        Assert.Equal("[legacy-servers]", File.ReadAllText(Path.Combine(_portableDir, "saved-servers.json")));
    }

    [Fact]
    public void Migrate_WhenPortableFileAlreadyExists_ShouldKeepPortableVersion()
    {
        // Given the portable dir already has a settings file, When migrating, Then the portable file wins.
        File.WriteAllText(Path.Combine(_legacyDir, "launcher-settings.json"), "{legacy-settings}");
        File.WriteAllText(Path.Combine(_portableDir, "launcher-settings.json"), "{portable-settings}");
        LegacyDataMigration.Migrate(_legacyDir, PortableFiles());

        Assert.Equal("{portable-settings}", File.ReadAllText(Path.Combine(_portableDir, "launcher-settings.json")));
    }

    [Fact]
    public void Migrate_WhenLegacyMissing_ShouldNotCreateFiles()
    {
        // Given no legacy files exist, When migrating, Then nothing appears in the portable dir.
        LegacyDataMigration.Migrate(_legacyDir, PortableFiles());

        Assert.Empty(Directory.GetFiles(_portableDir));
    }

    [Fact]
    public void Migrate_ShouldLeaveLegacyFilesIntactAsBackup()
    {
        // Given legacy files exist, When migrating, Then legacy is untouched afterward.
        File.WriteAllText(Path.Combine(_legacyDir, "launcher-settings.json"), "{legacy-settings}");
        LegacyDataMigration.Migrate(_legacyDir, PortableFiles());

        Assert.True(File.Exists(Path.Combine(_legacyDir, "launcher-settings.json")));
    }

    [Fact]
    public void Migrate_WhenCopyThrows_ShouldSwallowAndStillCopyTheOtherFile()
    {
        // Given the copy of one file throws (locked/unauthorized), When migrating, Then that failure is
        // swallowed (best-effort) and the remaining files still migrate.
        File.WriteAllText(Path.Combine(_legacyDir, "launcher-settings.json"), "{legacy-settings}");
        File.WriteAllText(Path.Combine(_legacyDir, "saved-servers.json"), "[legacy-servers]");
        LegacyDataMigration.Migrate(_legacyDir, PortableFiles(), CopyFile);

        Assert.Equal("[legacy-servers]", File.ReadAllText(Path.Combine(_portableDir, "saved-servers.json")));
        Assert.False(File.Exists(Path.Combine(_portableDir, "launcher-settings.json")));
    }

    [Fact]
    public void Migrate_WhenLegacyFileMissingPortableStillEmpty_ShouldSkipSilently()
    {
        // Given only saved-servers.json exists in legacy, When migrating, Then the missing settings file is
        // skipped and the present one copies.
        File.WriteAllText(Path.Combine(_legacyDir, "saved-servers.json"), "[legacy-servers]");
        LegacyDataMigration.Migrate(_legacyDir, PortableFiles());

        Assert.Equal("[legacy-servers]", File.ReadAllText(Path.Combine(_portableDir, "saved-servers.json")));
        Assert.False(File.Exists(Path.Combine(_portableDir, "launcher-settings.json")));
    }

    private static void CopyFile(string source, string destination)
    {
        if (destination.EndsWith("launcher-settings.json"))
        {
            throw new UnauthorizedAccessException("simulated locked legacy file");
        }

        File.Copy(source, destination);
    }

    private string[] PortableFiles()
    {
        return
        [
            Path.Combine(_portableDir, "launcher-settings.json"),
            Path.Combine(_portableDir, "saved-servers.json"),
        ];
    }
}