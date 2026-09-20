using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Configuration;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class ConfigurationRepositoryTests
{
    [Fact]
    public void SaveAndLoad_ShouldPreserveSettings()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var repository = new ConfigurationRepository(storage);

        var original = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        // When
        repository.Save(original);

        var loaded = repository.Load();

        // Then
        Assert.Equal(original.PreferredClient, loaded.PreferredClient);
        Assert.Equal(original.AutoLaunch, loaded.AutoLaunch);
    }

    [Fact]
    public void Load_WhenNoSettingsExist_ShouldReturnDefaultSettings()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var repository = new ConfigurationRepository(storage);

        // When
        var settings = repository.Load();

        // Then
        Assert.Equal(GameClient.FiveM, settings.PreferredClient);
        Assert.False(settings.AutoLaunch);
    }
    [Fact]
    public void Save_WhenSettingsAreNull_ShouldThrowArgumentNullException()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var repository = new ConfigurationRepository(storage);


        // When / Then
        Assert.Throws<ArgumentNullException>(() => repository.Save(null!));
    }
}

