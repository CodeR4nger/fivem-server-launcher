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
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        // Act
        repository.Save(original);

        var loaded = repository.Load();

        // Assert
        Assert.Equal(original.PreferredClient, loaded.PreferredClient);
        Assert.Equal(original.AutoLaunch, loaded.AutoLaunch);
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
        Assert.Equal(GameClient.FiveM, settings.PreferredClient);
        Assert.False(settings.AutoLaunch);
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

