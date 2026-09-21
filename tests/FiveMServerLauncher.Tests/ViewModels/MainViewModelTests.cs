using System.Net;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.Tests.Domain;
using FiveMServerLauncher.ViewModels;
using GameLauncherType = FiveMServerLauncher.Launch.GameLauncher;
using IGameProcessLauncher = FiveMServerLauncher.Launch.IGameProcessLauncher;

namespace FiveMServerLauncher.Tests.ViewModels;

public class MainViewModelTests
{
    [Fact]
    public async Task ConnectAsync_WithValidCfxJoinAddress_ShouldLaunchConnectUri()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Single(processLauncher.Requests);
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95", processLauncher.Requests[0].AbsoluteUri);
        Assert.False(vm.IsBusy);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidAddress_ShouldShowErrorWithoutLaunching()
    {
        // Given
        const string invalidAddress = "9 92";
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));
        vm.ServerAddress = invalidAddress;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Invalid address", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WithEnhancedServer_ShouldShowOpenClientWithoutLaunching()
    {
        // Given
        const string address = "cfx.re/join/ecxx01";
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5enhanced"));
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Opening FiveMEnhanced...", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WhenStartFails_ShouldShowStartFailedText()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher { ThrowOnStart = true };
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Launch failed", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamRequiredButNotRunning_ShouldShowRequirementStatusWithoutLaunching()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var readiness = new FakeRequirementReadiness { Running = false };
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Requires Steam (not running)", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
        Assert.Equal(1, readiness.SteamChecks);
        Assert.Equal(0, readiness.DiscordChecks);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamRequiredAndRunning_ShouldLaunchNormally()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var readiness = new FakeRequirementReadiness { Running = true };
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
        Assert.Equal(1, readiness.SteamChecks);
    }

    [Fact]
    public async Task ConnectAsync_WhenTypedAddressMatchesSavedServer_ShouldApplyManualFlags()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", address, requiresDiscord: true));
        var readiness = new FakeRequirementReadiness { Running = false };
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5"),
            repository: repository,
            readiness: readiness);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Requires Discord (not running)", vm.StatusText);
        Assert.Empty(processLauncher.Requests);
        Assert.Equal(1, readiness.DiscordChecks);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamAndDiscordRequiredAndMissing_ShouldListBoth()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", address, requiresDiscord: true));
        var readiness = new FakeRequirementReadiness { Running = false };
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            repository: repository,
            readiness: readiness);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Requires Steam (not running) / Requires Discord (not running)", vm.StatusText);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WhenNoRequirementsApply_ShouldLaunchWithoutReadinessChecks()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var readiness = new FakeRequirementReadiness();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), readiness: readiness);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
        Assert.Equal(0, readiness.SteamChecks);
        Assert.Equal(0, readiness.DiscordChecks);
    }

    [Fact]
    public async Task ConnectAsync_WhenInvalidAddress_ShouldNotCheckReadiness()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var readiness = new FakeRequirementReadiness();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), readiness: readiness);
        vm.ServerAddress = "9 92";

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Invalid address", vm.StatusText);
        Assert.Empty(processLauncher.Requests);
        Assert.Equal(0, readiness.SteamChecks);
        Assert.Equal(0, readiness.DiscordChecks);
    }

    private static string CfxJson(string gamename, string? extraVars = null)
    {
        var vars = $"\"gamename\":\"{gamename}\"";
        if (!string.IsNullOrEmpty(extraVars))
        {
            vars += $",{extraVars}";
        }

        return $"{{\"data\":{{\"sv_projectName\":\"Test Server\",\"vars\":{{{vars}}}}}}}";
    }

    private static MainViewModel CreateViewModel(
        IGameProcessLauncher processLauncher,
        string cfxJson,
        IServerRepository? repository = null,
        FakeRequirementReadiness? readiness = null)
    {
        var resolver = new ServerResolver(
            new CfxService(
                new HttpClient(
                    new FakeHttpMessageHandler(HttpStatusCode.OK, cfxJson))),
            new ServerCatalog(new HttpClient(new FakeHttpMessageHandler(true))),
            new ServerRequirementsResolver(),
            new FakeDnsResolver());

        var launcher = new GameLauncherType(processLauncher, new FakeCitizenFxPreparer());

        return new MainViewModel(
            resolver,
            launcher,
            repository ?? new InMemoryServerRepository(),
            readiness ?? new FakeRequirementReadiness());
    }
}