using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class FileSettingsStorageTests
{
    [Fact]
    public void Save_ShouldCreateSettingsFile()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var storage = new FileSettingsStorage(tempDir.FilePath);

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        // When
        storage.Save(settings);

        // Then
        Assert.True(File.Exists(tempDir.FilePath));
    }

    [Fact]
    public void Save_ShouldPersistSettingsAsJson()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var storage = new FileSettingsStorage(tempDir.FilePath);

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        // When
        storage.Save(settings);

        var json = File.ReadAllText(tempDir.FilePath);

        // Then
        Assert.Contains("\"PreferredClient\":\"FiveMEnhanced\"", json);
        Assert.Contains("\"AutoLaunch\":true", json);
    }

    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldReturnNull()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        var result = storage.Load();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public void Load_WhenFileContainsInvalidJson_ShouldReturnNull()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, "{ invalid json");

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        var result = storage.Load();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public void Load_WhenJsonCannotBeDeserialized_ShouldReturnNull()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);

        var json = """
                    {
                        "PreferredClient": "RedMEnhanced",
                        "AutoLaunch": true,
                    }
                    """;

        File.WriteAllText(tempDir.FilePath, json);

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        var result = storage.Load();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public void Load_WhenFileIsEmpty_ShouldReturnNull()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, string.Empty);

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        var result = storage.Load();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public void Save_WhenDirectoryDoesNotExist_ShouldCreateDirectoryAndPersistSettings()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveM,
            AutoLaunch = true
        };

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        storage.Save(settings);

        // Then
        Assert.True(File.Exists(tempDir.FilePath));
    }

    [Fact]
    public void Save_WhenFileAlreadyExists_ShouldOverwriteExistingSettings()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();

        var firstSettings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveM,
            AutoLaunch = false
        };

        var secondSettings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true
        };

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        storage.Save(firstSettings);
        storage.Save(secondSettings);

        // Then
        var result = storage.Load();

        Assert.NotNull(result);
        Assert.Equal(GameClient.FiveMEnhanced, result.PreferredClient);
        Assert.True(result.AutoLaunch);
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveDevModeFields()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();

        var settings = new LauncherSettings
        {
            DevGameBuild = 3095,
            DevPureMode = 1,
        };

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        storage.Save(settings);
        var result = storage.Load();

        // Then
        Assert.NotNull(result);
        Assert.Equal(3095, result.DevGameBuild);
        Assert.Equal(1, result.DevPureMode);
    }

    [Fact]
    public void Load_WhenFileLacksDevModeFields_ShouldReturnNullFields()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);

        File.WriteAllText(tempDir.FilePath, """{"PreferredClient":"FiveM"}""");

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        var result = storage.Load();

        // Then
        Assert.NotNull(result);
        Assert.Null(result.DevGameBuild);
        Assert.Null(result.DevPureMode);
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveLastServerAddress()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
            LastServerAddress = "cfx.re/join/y4lg95",
        };

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        storage.Save(settings);
        var result = storage.Load();

        // Then
        Assert.NotNull(result);
        Assert.Equal(settings.LastServerAddress, result.LastServerAddress);
    }

    [Fact]
    public void Load_WhenFileLacksLastServerAddress_ShouldReturnNullField()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);

        var json = """
                    {
                        "PreferredClient": "FiveMEnhanced",
                        "AutoLaunch": true
                    }
                    """;

        File.WriteAllText(tempDir.FilePath, json);

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        var result = storage.Load();

        // Then
        Assert.NotNull(result);
        Assert.Equal(GameClient.FiveMEnhanced, result.PreferredClient);
        Assert.True(result.AutoLaunch);
        Assert.Null(result.LastServerAddress);
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveAllSettings()
    {
        // Given
        using var tempDir = new TempSettingsDirectory();

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        var storage = new FileSettingsStorage(tempDir.FilePath);

        // When
        storage.Save(settings);
        var result = storage.Load();

        // Then
        Assert.NotNull(result);
        Assert.Equal(settings.PreferredClient, result.PreferredClient);
        Assert.Equal(settings.AutoLaunch, result.AutoLaunch);
    }
}