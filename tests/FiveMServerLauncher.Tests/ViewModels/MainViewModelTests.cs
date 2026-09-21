using System.Net;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.Tests.Domain;
using FiveMServerLauncher.ViewModels;

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
    public async Task ConnectAsync_WhenSteamRequiredAndRunning_ShouldLaunchWithoutStartingAnything()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var readiness = new FakeRequirementReadiness { Running = true };
        var starter = new FakeExternalAppStarter();
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness,
            starter: starter);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Empty(starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
        Assert.Equal(0, readiness.SteamChecks);
        Assert.Equal(1, readiness.SteamReadyChecks);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamAlreadyReady_ShouldNotWaitOrShowStartStatus()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var starter = new FakeExternalAppStarter();
        var statusDuringPrepare = new List<string>();
        MainViewModel? vm = null;
        var readiness = new FakeRequirementReadiness { Running = true, Ready = true };
        var preparer = new ExternalAppPreparer(
            readiness,
            starter,
            () =>
            {
                statusDuringPrepare.Add(vm!.StatusText);
                return Task.CompletedTask;
            },
            maxAttempts: 5);
        vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness,
            preparer: preparer);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Empty(starter.Starts);
        Assert.Empty(statusDuringPrepare);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamRequiredAndMissing_ShouldStartSteamAndLaunch()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var starter = new FakeExternalAppStarter();
        var readiness = new StatefulRequirementReadiness(app => starter.Starts.Contains(app));
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness,
            starter: starter);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal([ExternalApp.Steam], starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WhenManualSteamTrueWithoutPublishedRequirement_ShouldStartSteamAndLaunch()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var (vm, starter) = CreateManualSteamContext(processLauncher, CfxJson("gta5"));

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal([ExternalApp.Steam], starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WhenManualSteamTrueOverridesPublishedFalse_ShouldStartSteamAndLaunch()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var (vm, starter) = CreateManualSteamContext(
            processLauncher, CfxJson("gta5", "\"sv_enforceSteamAuth\":\"false\""));

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal([ExternalApp.Steam], starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamRunningButNotReady_ShouldWaitAndLaunchWithoutRestarting()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var starter = new FakeExternalAppStarter();
        var checks = 0;
        var readiness = new StatefulRequirementReadiness(_ => true, _ => ++checks >= 3);
        var statusDuringPrepare = new List<string>();
        MainViewModel? vm = null;
        var preparer = new ExternalAppPreparer(
            readiness,
            starter,
            () =>
            {
                statusDuringPrepare.Add(vm!.StatusText);
                return Task.CompletedTask;
            },
            maxAttempts: 5);
        vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness,
            preparer: preparer);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Empty(starter.Starts);
        Assert.Equal(["Starting Steam..."], statusDuringPrepare);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamRequiredAndMissing_ShouldShowStartingSteamWhilePreparing()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var starter = new FakeExternalAppStarter();
        var checks = 0;
        var readiness = new StatefulRequirementReadiness(_ => ++checks > 3);
        var statusDuringPrepare = new List<string>();
        MainViewModel? vm = null;
        var preparer = new ExternalAppPreparer(
            readiness,
            starter,
            () =>
            {
                statusDuringPrepare.Add(vm!.StatusText);
                return Task.CompletedTask;
            },
            maxAttempts: 5);
        vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness,
            preparer: preparer);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal(["Starting Steam..."], statusDuringPrepare);
        Assert.Equal([ExternalApp.Steam], starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WhenRequiredAppNeverBecomesRunning_ShouldShowCouldNotStartAndNotLaunch()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var readiness = new StatefulRequirementReadiness(_ => false);
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Could not start Steam", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WhenRequiredAppCannotBeStarted_ShouldShowCouldNotStartAndNotLaunch()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var readiness = new FakeRequirementReadiness { Running = false };
        var starter = new FakeExternalAppStarter
        {
            ThrowOnStart = new System.ComponentModel.Win32Exception("no association")
        };
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            readiness: readiness,
            starter: starter);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Could not start Steam", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task ConnectAsync_WhenTypedAddressMatchesSavedServer_ShouldStartDiscordAndLaunch()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", address, requiresDiscord: true));
        var starter = new FakeExternalAppStarter();
        var readiness = new StatefulRequirementReadiness(app => starter.Starts.Contains(app));
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5"),
            repository: repository,
            readiness: readiness,
            starter: starter);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal([ExternalApp.Discord], starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    [Fact]
    public async Task ConnectAsync_WhenSteamAndDiscordRequiredAndMissing_ShouldPrepareBothThenLaunch()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var processLauncher = new FakeGameProcessLauncher();
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", address, requiresDiscord: true));
        var starter = new FakeExternalAppStarter();
        var readiness = new StatefulRequirementReadiness(app => starter.Starts.Contains(app));
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5", "\"sv_enforceSteamAuth\":\"true\""),
            repository: repository,
            readiness: readiness,
            starter: starter);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal([ExternalApp.Steam, ExternalApp.Discord], starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
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

    [Fact]
    public void Ctor_ShouldLoadSavedServersFromRepository()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "abc123", requiresSteam: true));

        // When
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.Equal("My Server", item.Name);
        Assert.Equal("abc123", item.Address);
        Assert.True(item.RequiresSteam);
    }

    [Fact]
    public void SaveServer_WhenValid_ShouldAddToCollectionAndRepository()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);
        vm.NewServerName = "My Server";
        vm.NewServerAddress = "cfx.re/join/abc123";

        // When
        vm.AddServerCommand.Execute(null);

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.Equal("My Server", item.Name);
        Assert.Single(repository.GetAll());
        Assert.Equal("Server saved", vm.StatusText);
    }

    [Fact]
    public void SaveServer_WhenInvalidAddress_ShouldShowInvalidStatusWithoutSaving()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);
        vm.NewServerName = "My Server";
        vm.NewServerAddress = "9 92";

        // When
        vm.AddServerCommand.Execute(null);

        // Then
        Assert.Equal("Invalid name or address", vm.StatusText);
        Assert.Empty(repository.GetAll());
        Assert.Empty(vm.SavedServers);
    }

    [Fact]
    public void SaveServer_WhenAddressAlreadySaved_ShouldShowAlreadySavedStatus()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("First", "abc123"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);
        vm.NewServerName = "Second";
        vm.NewServerAddress = "abc123";

        // When
        vm.AddServerCommand.Execute(null);

        // Then
        Assert.Equal("Server already saved", vm.StatusText);
        Assert.Single(repository.GetAll());
    }

    [Fact]
    public void SelectSavedServer_ShouldFillConnectAddress()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/abc123"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);

        // When
        vm.SelectedServer = vm.SavedServers[0];

        // Then
        Assert.Equal("cfx.re/join/abc123", vm.ServerAddress);
    }

    [Fact]
    public void DeleteSelectedSavedServer_ShouldRemoveFromCollectionAndRepository()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/abc123"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);
        vm.SelectedServer = vm.SavedServers[0];

        // When
        vm.DeleteServerCommand.Execute(null);

        // Then
        Assert.Empty(vm.SavedServers);
        Assert.Empty(repository.GetAll());
        Assert.Null(vm.SelectedServer);
    }

    [Fact]
    public void ToggleRequiresSteamOnSavedItem_ShouldPersistManualFlag()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "abc123"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);
        var item = vm.SavedServers[0];

        // When
        item.RequiresSteam = true;

        // Then
        var saved = Assert.Single(repository.GetAll());
        Assert.True(saved.RequiresSteam);
    }

    [Fact]
    public async Task Connect_WhenSelectedSavedServerRequiresMissingApp_ShouldPrepareThenLaunch()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", address, requiresDiscord: true));
        var processLauncher = new FakeGameProcessLauncher();
        var starter = new FakeExternalAppStarter();
        var readiness = new StatefulRequirementReadiness(app => starter.Starts.Contains(app));
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5"),
            repository: repository,
            readiness: readiness,
            starter: starter);
        vm.SelectedServer = vm.SavedServers[0];

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal(address, vm.ServerAddress);
        Assert.Equal([ExternalApp.Discord], starter.Starts);
        Assert.Single(processLauncher.Requests);
        Assert.Equal("Launching FiveM...", vm.StatusText);
    }

    private static (MainViewModel Vm, FakeExternalAppStarter Starter) CreateManualSteamContext(
        FakeGameProcessLauncher processLauncher,
        string cfxJson)
    {
        const string address = "cfx.re/join/y4lg95";
        var starter = new FakeExternalAppStarter();
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", address, requiresSteam: true));
        var vm = CreateViewModel(
            processLauncher,
            cfxJson,
            repository: repository,
            readiness: new StatefulRequirementReadiness(app => starter.Starts.Contains(app)),
            starter: starter);
        vm.ServerAddress = address;
        return (vm, starter);
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
        IRequirementReadiness? readiness = null,
        FakeExternalAppStarter? starter = null,
        ExternalAppPreparer? preparer = null)
    {
        var resolver = new ServerResolver(
            new CfxService(
                new HttpClient(
                    new FakeHttpMessageHandler(HttpStatusCode.OK, cfxJson))),
            new ServerCatalog(new HttpClient(new FakeHttpMessageHandler(true))),
            new ServerRequirementsResolver(),
            new FakeDnsResolver());

        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer());

        var readinessValue = readiness ?? new FakeRequirementReadiness();
        var preparerValue = preparer ?? new ExternalAppPreparer(
            readinessValue,
            starter ?? new FakeExternalAppStarter(),
            () => Task.CompletedTask,
            maxAttempts: 5);

        return new MainViewModel(
            resolver,
            launcher,
            repository ?? new InMemoryServerRepository(),
            preparerValue);
    }
}