using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

public class GameLauncherTests
{
    [Fact]
    public async Task ConnectAsync_WithValidatedCfxProfile_ShouldLaunchConnectUri()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements { GameBuild = 3258, PureMode = 1 },
        };

        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(processLauncher);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        var connect = Assert.IsType<LaunchResult.Connect>(result);
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1", connect.ConnectUri.AbsoluteUri);
        Assert.Single(processLauncher.Requests);
        Assert.Equal(connect.ConnectUri, processLauncher.Requests[0]);
    }

    [Fact]
    public async Task ConnectAsync_WithEnhancedProfile_ShouldReturnOpenClientWithoutLaunching()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveMEnhanced,
            Requirements = new ServerRequirements(),
        };

        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(processLauncher);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        var openClient = Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(GameClient.FiveMEnhanced, openClient.GameClient);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WhenProcessLauncherThrows_ShouldReturnStartFailed()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements(),
            IsCfxValidated = true,
        };

        var processLauncher = new FakeGameProcessLauncher { ThrowOnStart = true };
        var launcher = new GameLauncher(processLauncher);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        Assert.IsType<LaunchResult.StartFailed>(result);
        Assert.Empty(processLauncher.Requests);
    }
}