using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Configuration;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class InMemorySettingsStorageTests
{
    [Fact]
    public void SaveAndLoad_ShouldPreserveSettings()
    {
        // Arrange
        var storage = new InMemorySettingsStorage();

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        // Act
        storage.Save(settings);

        var loaded = storage.Load();

        // Assert
        Assert.Equal(settings.PreferredClient, loaded!.PreferredClient);
        Assert.Equal(settings.AutoLaunch, loaded.AutoLaunch);
    }
}
