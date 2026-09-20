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
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        try
        {
            // When
            storage.Save(settings);

            // Then
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void Save_ShouldPersistSettingsAsJson()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };
        try
        {
            // When
            storage.Save(settings);

            var json = File.ReadAllText(filePath);

            // Then
            Assert.Contains("\"PreferredClient\":\"FiveMEnhanced\"", json);
            Assert.Contains("\"AutoLaunch\":true", json);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void Load_ShouldRestoreSavedSettings()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        var original = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };
        try
        {
            // When
            storage.Save(original);

            var loaded = storage.Load();

            // Then
            Assert.NotNull(loaded);
            Assert.Equal(original.PreferredClient, loaded.PreferredClient);
            Assert.Equal(original.AutoLaunch, loaded.AutoLaunch);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldReturnNull()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        // When
        var result = storage.Load();

        // Then
        Assert.Null(result);
    }
    [Fact]
    public void Load_WhenFileContainsInvalidJson_ShouldReturnNull()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        Directory.CreateDirectory(directory);

        File.WriteAllText(filePath, "{ invalid json");

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // When
            var result = storage.Load();

            // Then
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void Load_WhenJsonCannotBeDeserialized_ShouldReturnNull()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        Directory.CreateDirectory(directory);

        var json = """
                {
                    "PreferredClient": "RedMEnhanced",
                    "AutoLaunch": true,
                }
                """;

        File.WriteAllText(filePath, json);

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // When
            var result = storage.Load();

            // Then
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void Load_WhenFileIsEmpty_ShouldReturnNull()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        Directory.CreateDirectory(directory);

        File.WriteAllText(filePath, string.Empty);

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // When
            var result = storage.Load();

            // Then
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void Save_WhenDirectoryDoesNotExist_ShouldCreateDirectoryAndPersistSettings()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveM,
            AutoLaunch = true
        };

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // When
            storage.Save(settings);

            // Then
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void Save_WhenFileAlreadyExists_ShouldOverwriteExistingSettings()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

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

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // When
            storage.Save(firstSettings);
            storage.Save(secondSettings);

            // Then
            var result = storage.Load();

            Assert.NotNull(result);
            Assert.Equal(GameClient.FiveMEnhanced, result.PreferredClient);
            Assert.True(result.AutoLaunch);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
    [Fact]
    public void SaveAndLoad_ShouldPreserveAllSettings()
    {
        // Given
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // When
            storage.Save(settings);
            var result = storage.Load();

            // Then
            Assert.NotNull(result);
            Assert.Equal(settings.PreferredClient, result.PreferredClient);
            Assert.Equal(settings.AutoLaunch, result.AutoLaunch);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

}
