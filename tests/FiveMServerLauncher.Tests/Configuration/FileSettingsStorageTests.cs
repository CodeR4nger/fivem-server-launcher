using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class FileSettingsStorageTests
{
    [Fact]
    public void Save_ShouldCreateSettingsFile()
    {
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        var settings = new LauncherSettings
        {
            Platform = GamePlatform.Epic,
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
            ServerPort = 30121
        };

        try
        {
            // Act
            storage.Save(settings);

            // Assert
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        var settings = new LauncherSettings
        {
            Platform = GamePlatform.Epic,
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
            ServerPort = 30121
        };
        try
        {
            // Act
            storage.Save(settings);

            var json = File.ReadAllText(filePath);

            // Assert
            Assert.Contains("\"Platform\":\"Epic\"", json);
            Assert.Contains("\"PreferredClient\":\"FiveMEnhanced\"", json);
            Assert.Contains("\"AutoLaunch\":true", json);
            Assert.Contains("\"ServerPort\":30121", json);
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        var original = new LauncherSettings
        {
            Platform = GamePlatform.Epic,
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
            ServerPort = 30121
        };
        try
        {
            // Act
            storage.Save(original);

            var loaded = storage.Load();

            // Assert
            Assert.NotNull(loaded);
            Assert.Equal(original.Platform, loaded.Platform);
            Assert.Equal(original.PreferredClient, loaded.PreferredClient);
            Assert.Equal(original.AutoLaunch, loaded.AutoLaunch);
            Assert.Equal(original.ServerPort, loaded.ServerPort);
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var storage = new FileSettingsStorage(filePath);

        // Act
        var result = storage.Load();

        // Assert
        Assert.Null(result);
    }
    [Fact]
    public void Load_WhenFileContainsInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        Directory.CreateDirectory(directory);

        File.WriteAllText(filePath, "{ invalid json");

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // Act
            var result = storage.Load();

            // Assert
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        Directory.CreateDirectory(directory);

        var json = """
                {
                    "Platform": "InvalidPlatform",
                    "PreferredClient": "FiveMEnhanced",
                    "AutoLaunch": true,
                    "ServerPort": 30121
                }
                """;

        File.WriteAllText(filePath, json);

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // Act
            var result = storage.Load();

            // Assert
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        Directory.CreateDirectory(directory);

        File.WriteAllText(filePath, string.Empty);

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // Act
            var result = storage.Load();

            // Assert
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var settings = new LauncherSettings
        {
            Platform = GamePlatform.Epic,
            PreferredClient = GameClient.FiveM,
            AutoLaunch = true
        };

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // Act
            storage.Save(settings);

            // Assert
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var firstSettings = new LauncherSettings
        {
            Platform = GamePlatform.Steam,
            PreferredClient = GameClient.FiveM,
            AutoLaunch = false
        };

        var secondSettings = new LauncherSettings
        {
            Platform = GamePlatform.Epic,
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true
        };

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // Act
            storage.Save(firstSettings);
            storage.Save(secondSettings);

            // Assert
            var result = storage.Load();

            Assert.NotNull(result);
            Assert.Equal(GamePlatform.Epic, result.Platform);
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
        // Arrange
        var directory = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        var filePath = Path.Combine(directory, "settings.json");

        var settings = new LauncherSettings
        {
            Platform = GamePlatform.Epic,
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
            ServerPort = 30121
        };

        var storage = new FileSettingsStorage(filePath);

        try
        {
            // Act
            storage.Save(settings);
            var result = storage.Load();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings.Platform, result.Platform);
            Assert.Equal(settings.PreferredClient, result.PreferredClient);
            Assert.Equal(settings.AutoLaunch, result.AutoLaunch);
            Assert.Equal(settings.ServerPort, result.ServerPort);
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
