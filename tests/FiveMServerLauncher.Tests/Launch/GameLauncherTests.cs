using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Tests.Launch;

public class GameLauncherTests
{
    [Fact]
    public async Task ConnectAsync_WithValidationCfxProfile_ShouldLaunchConnectUri()
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
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1", result.ConnectUri?.AbsoluteUri);
        Assert.Single(processLauncher.Requests);
        Assert.Equal(result.ConnectUri, processLauncher.Requests[0]);
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
        Assert.Null(result.ConnectUri);
        Assert.Equal(GameClient.FiveMEnhanced, result.GameClient);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WithUnvalidatedIpPortProfile_ShouldLaunchDirectConnectUri()
    {
        // Given
        var profile = new ServerProfile
        {
            Address = "149.56.120.52:30320",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements(),
            IsCfxValidated = false,
        };

        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(processLauncher);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        Assert.Equal("fivem://connect/149.56.120.52:30320", result.ConnectUri?.AbsoluteUri);
        Assert.Single(processLauncher.Requests);
        Assert.Equal(result.ConnectUri, processLauncher.Requests[0]);
    }
}