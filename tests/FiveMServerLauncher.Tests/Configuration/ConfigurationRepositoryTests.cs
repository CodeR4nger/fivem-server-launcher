using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Configuration;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class ConfigurationRepositoryTests
{
    [Fact]
    public void SaveAndLoad_ShouldPreserveSettings()
    {
        // Arrange
        var storage = new InMemorySettingsStorage();
        var repository = new ConfigurationRepository(storage);

        var original = new LauncherSettings
        {
            Platform = GamePlatform.Epic,
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
            ServerPort = 30121
        };

        // Act
        repository.Save(original);

        var loaded = repository.Load();

        // Assert
        Assert.Equal(original.Platform, loaded.Platform);
        Assert.Equal(original.PreferredClient, loaded.PreferredClient);
        Assert.Equal(original.AutoLaunch, loaded.AutoLaunch);
        Assert.Equal(original.ServerPort, loaded.ServerPort);
    }

    [Fact]
    public void Load_WhenNoSettingsExist_ShouldReturnDefaultSettings()
    {
        // Arrange
        var storage = new InMemorySettingsStorage();
        var repository = new ConfigurationRepository(storage);

        // Act
        var settings = repository.Load();

        // Assert
        Assert.Equal(GamePlatform.Steam, settings.Platform);
        Assert.Equal(GameClient.FiveM, settings.PreferredClient);
        Assert.False(settings.AutoLaunch);
        Assert.Equal(30120, settings.ServerPort);
    }
    [Fact]
    public void Save_WhenSettingsAreNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var storage = new InMemorySettingsStorage();
        var repository = new ConfigurationRepository(storage);


        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => repository.Save(null!));
    }
}

