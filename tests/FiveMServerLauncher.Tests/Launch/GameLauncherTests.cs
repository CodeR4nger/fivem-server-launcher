using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

public class GameLauncherTests
{
    private const string EnhancedExecutablePath = @"C:\FiveM for GTAV Enhanced\FiveM.exe";

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

        public Task StartExecutableAsync(string executablePath)
        {
            _log.Add("launch");
            return Task.CompletedTask;
        }

        public Task StartExecutableAsync(string executablePath, IReadOnlyList<string> arguments)
        {
            _log.Add("launch");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task OpenAsync_WithDevOptions_ShouldStartExeWithArgs()
    {
        // Given
        var options = FiveMLaunchOptions.Create(
            address: null,
            gameClient: GameClient.FiveM,
            gameBuild: 3095,
            pureMode: 1,
            secondClient: false);

        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), new FakeClientInstallLocator());

        // When
        var result = await launcher.OpenAsync(options);

        // Then
        var openClient = Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(GameClient.FiveM, openClient.GameClient);
        var start = Assert.Single(processLauncher.ExecutableArgsStarts);
        Assert.Equal(@"C:\FiveM\FiveM.app\FiveM.exe", start.Path);
        Assert.Equal(["-b3095", "-pure_1"], start.Args);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task OpenAsync_WithDevOptionsAndSecondClient_ShouldIncludeCl2()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, null, null, secondClient: true);

        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), new FakeClientInstallLocator());

        // When
        var result = await launcher.OpenAsync(options);

        // Then
        Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(["-cl2"], Assert.Single(processLauncher.ExecutableArgsStarts).Args);
    }

    [Fact]
    public async Task OpenAsync_WithDevOptions_WhenNotInstalled_ShouldReturnNotInstalled()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, null, null, false);
        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(
            processLauncher,
            new FakeCitizenFxPreparer(),
            new FakeClientInstallLocator { Executables = [] });

        // When
        var result = await launcher.OpenAsync(options);

        // Then
        Assert.IsType<LaunchResult.NotInstalled>(result);
        Assert.Empty(processLauncher.ExecutableArgsStarts);
    }

    [Fact]
    public async Task OpenAsync_WithDevOptions_WhenStartThrows_ShouldReturnStartFailed()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3095, null, false);
        var processLauncher = new FakeGameProcessLauncher { ThrowOnExecutableStart = true };
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), new FakeClientInstallLocator());

        // When
        var result = await launcher.OpenAsync(options);

        // Then
        Assert.IsType<LaunchResult.StartFailed>(result);
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
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), new FakeClientInstallLocator());

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
        var launcher = new GameLauncher(new OrderedProcessLauncher(log), new OrderedPreparer(log), new FakeClientInstallLocator());

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        Assert.IsType<LaunchResult.Connect>(result);
        Assert.Equal(["prime", "launch"], log);
    }

    [Fact]
    public async Task ConnectAsync_WithEnhancedProfile_ShouldStillPrimeBeforeOpeningClient()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveMEnhanced,
            Requirements = new ServerRequirements(),
        };

        var log = new List<string>();
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var launcher = new GameLauncher(
            new OrderedProcessLauncher(log),
            new OrderedPreparer(log),
            locator);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(["prime", "launch"], log);
    }

    [Fact]
    public async Task ConnectAsync_WithEnhancedProfile_ShouldStartEnhancedExecutableAndReturnOpenClient()
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
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), locator);

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        var openClient = Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(GameClient.FiveMEnhanced, openClient.GameClient);
        Assert.Equal(EnhancedExecutablePath, Assert.Single(processLauncher.ExecutableStarts));
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WithEnhancedProfile_WhenEnhancedNotInstalled_ShouldReturnNotInstalled()
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
        var launcher = new GameLauncher(
            processLauncher,
            new FakeCitizenFxPreparer(),
            new FakeClientInstallLocator { Executables = [] });

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        var notInstalled = Assert.IsType<LaunchResult.NotInstalled>(result);
        Assert.Equal(GameClient.FiveMEnhanced, notInstalled.GameClient);
        Assert.Empty(processLauncher.ExecutableStarts);
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
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), new FakeClientInstallLocator());

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
        var launcher = new GameLauncher(processLauncher, preparer, new FakeClientInstallLocator());

        // When
        var result = await launcher.ConnectAsync(profile);

        // Then
        var connect = Assert.IsType<LaunchResult.Connect>(result);
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95?-b3258", connect.ConnectUri.AbsoluteUri);
        Assert.Single(processLauncher.Requests);
        Assert.Empty(preparer.PrimedProfiles);
    }

    [Fact]
    public async Task OpenAsync_WithInstalledLegacyClient_ShouldStartExecutableAndReturnOpenClient()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var launcher = new GameLauncher(
            processLauncher,
            new FakeCitizenFxPreparer(),
            new FakeClientInstallLocator());

        // When
        var result = await launcher.OpenAsync(GameClient.FiveM);

        // Then
        var openClient = Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(GameClient.FiveM, openClient.GameClient);
        Assert.Equal(@"C:\FiveM\FiveM.app\FiveM.exe", Assert.Single(processLauncher.ExecutableStarts));
    }

    [Fact]
    public async Task OpenAsync_WithInstalledEnhancedClient_ShouldStartEnhancedExecutableAndReturnOpenClient()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), locator);

        // When
        var result = await launcher.OpenAsync(GameClient.FiveMEnhanced);

        // Then
        var openClient = Assert.IsType<LaunchResult.OpenClient>(result);
        Assert.Equal(GameClient.FiveMEnhanced, openClient.GameClient);
        Assert.Equal(EnhancedExecutablePath, Assert.Single(processLauncher.ExecutableStarts));
    }

    [Fact]
    public async Task OpenAsync_WhenClientNotInstalled_ShouldReturnNotInstalledWithoutStarting()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var locator = new FakeClientInstallLocator { Executables = [] };
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), locator);

        // When
        var result = await launcher.OpenAsync(GameClient.FiveMEnhanced);

        // Then
        var notInstalled = Assert.IsType<LaunchResult.NotInstalled>(result);
        Assert.Equal(GameClient.FiveMEnhanced, notInstalled.GameClient);
        Assert.Empty(processLauncher.ExecutableStarts);
    }

    [Fact]
    public async Task OpenAsync_WhenStartThrows_ShouldReturnStartFailed()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher { ThrowOnExecutableStart = true };
        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), new FakeClientInstallLocator());

        // When
        var result = await launcher.OpenAsync(GameClient.FiveM);

        // Then
        Assert.IsType<LaunchResult.StartFailed>(result);
        Assert.Empty(processLauncher.ExecutableStarts);
    }
}
