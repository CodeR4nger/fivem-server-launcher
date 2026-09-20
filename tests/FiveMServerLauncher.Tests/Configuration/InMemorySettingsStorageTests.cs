using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Configuration;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class InMemorySettingsStorageTests
{
    [Fact]
    public void SaveAndLoad_ShouldPreserveSettings()
    {
        // Given
        var storage = new InMemorySettingsStorage();

        var settings = new LauncherSettings
        {
            PreferredClient = GameClient.FiveMEnhanced,
            AutoLaunch = true,
        };

        // When
        storage.Save(settings);

        var loaded = storage.Load();

        // Then
        Assert.Equal(settings.PreferredClient, loaded!.PreferredClient);
        Assert.Equal(settings.AutoLaunch, loaded.AutoLaunch);
    }
}
