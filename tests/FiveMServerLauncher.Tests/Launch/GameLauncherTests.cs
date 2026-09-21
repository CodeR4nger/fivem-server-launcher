using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

public class GameLauncherTests
{
    private sealed class OrderedPreparer : ICitizenFxPreparer
    {
        private readonly List<string> _log;

        public OrderedPreparer(List<string> log)
        {
            _log = log;
        }

        public Task PrimeAsync(ServerProfile profile)
        {
            _log.Add("prime");
            return Task.CompletedTask;
        }
    }

    private sealed class OrderedProcessLauncher : IGameProcessLauncher
    {
        private readonly List<string> _log;

        public OrderedProcessLauncher(List<string> log)
        {
            _log = log;
        }

        public Task StartAsync(Uri uri)
        {
            _log.Add("launch");
            return Task.CompletedTask;
        }
    }

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
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer());

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        var connect = Assert.IsType<LaunchResult.Connect>(result);
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1", connect.ConnectUri.AbsoluteUri);
        Assert.Single(processLauncher.Requests);
        Assert.Equal(connect.ConnectUri, processLauncher.Requests[0]);
    }

    [Fact]
    public async Task ConnectAsync_ShouldPrimeCitizenFxConfigBeforeLaunching()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements { DefaultBuild = 3788 },
            IsCfxValidated = true,
        };

        var log = new List<string>();
        var launcher = new GameLauncher(new OrderedProcessLauncher(log), new OrderedPreparer(log));

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        Assert.IsType<LaunchResult.Connect>(result);
        Assert.Equal(["prime", "launch"], log);
    }

    [Fact]
    public async Task ConnectAsync_WithEnhancedProfile_ShouldStillPrimeBeforeReturningOpenClient()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveMEnhanced,
            Requirements = new ServerRequirements(),
        };

        var preparer = new FakeCitizenFxPreparer();
        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(processLauncher, preparer);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(profile, Assert.Single(preparer.PrimedProfiles));
        Assert.Empty(processLauncher.Requests);
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
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer());

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
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer());

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        Assert.IsType<LaunchResult.StartFailed>(result);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WhenPreparerThrows_ShouldStillReturnConnectResult()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements { GameBuild = 3258 },
            IsCfxValidated = true,
        };

        var preparer = new FakeCitizenFxPreparer { Throw = true };
        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(processLauncher, preparer);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        var connect = Assert.IsType<LaunchResult.Connect>(result);
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95?-b3258", connect.ConnectUri.AbsoluteUri);
        Assert.Single(processLauncher.Requests);
        Assert.Empty(preparer.PrimedProfiles);
    }
}