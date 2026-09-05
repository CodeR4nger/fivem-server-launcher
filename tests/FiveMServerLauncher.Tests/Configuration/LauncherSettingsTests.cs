using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using Xunit;

namespace FiveMServerLauncher.Tests.Configuration;

public class LauncherSettingsTests
{
    [Fact]
    public void NewSettings_ShouldUseDefaultValues()
    {
        var settings = new LauncherSettings();

        Assert.Equal(GamePlatform.Steam, settings.Platform);
        Assert.Equal(GameClient.FiveM, settings.PreferredClient);
        Assert.False(settings.AutoLaunch);
        Assert.Equal(30120, settings.ServerPort);
    }
}
