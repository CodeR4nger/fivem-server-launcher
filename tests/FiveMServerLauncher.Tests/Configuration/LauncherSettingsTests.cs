using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class LauncherSettingsTests
{
    [Fact]
    public void NewSettings_ShouldUseDefaultValues()
    {
        // Given
        var settings = new LauncherSettings();

        // When / Then
        Assert.Equal(GameClient.FiveM, settings.PreferredClient);
        Assert.False(settings.AutoLaunch);
        Assert.Null(settings.LastServerAddress);
    }
}
