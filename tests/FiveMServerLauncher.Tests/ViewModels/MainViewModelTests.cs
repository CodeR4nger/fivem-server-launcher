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
    public async Task ConnectAsync_WithEnhancedServer_ShouldStartEnhancedExecutableAndShowOpenClientStatus()
    {
        // Given
        const string address = "cfx.re/join/ecxx01";
        var processLauncher = new FakeGameProcessLauncher();
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var vm = CreateViewModel(processLauncher, CfxJson("gta5enhanced"), installLocator: locator);
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Opening FiveM Enhanced...", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
        Assert.Equal(@"C:\FiveM for GTAV Enhanced\FiveM.exe", Assert.Single(processLauncher.ExecutableStarts));
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

    [Fact]
    public async Task Initialize_WithLegacyAndEnhancedInstalled_ShouldListBothAndSelectFirst()
    {
        // Given
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), installLocator: locator);

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(2, vm.AvailableOpenClients.Count);
        Assert.Equal("FiveM", vm.AvailableOpenClients[0].DisplayName);
        Assert.Equal("FiveM Enhanced", vm.AvailableOpenClients[1].DisplayName);
        Assert.Equal(GameClient.FiveM, vm.SelectedOpenClient?.Client);
        Assert.Equal("FiveM", vm.SelectedOpenClientLabel);
    }

    private const string EnhancedExecutablePath = @"C:\FiveM for GTAV Enhanced\FiveM.exe";

    [Fact]
    public async Task Initialize_WhenNothingInstalled_ShouldListNothingAndDisableOpenCommand()
    {
        // Given
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            installLocator: new FakeClientInstallLocator { Executables = [] });

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Empty(vm.AvailableOpenClients);
        Assert.Null(vm.SelectedOpenClient);
        Assert.False(vm.OpenClientCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpenClientCommand_WhenLegacyInstalled_ShouldStartLegacyExecutableAndDescribeResult()
    {
        // Given
        const string legacyPath = @"C:\FiveM\FiveM.app\FiveM.exe";
        var processLauncher = new FakeGameProcessLauncher();
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveM] = legacyPath;
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), installLocator: locator);
        await vm.InitializeAsync();

        // When
        await vm.OpenClientAsync();

        // Then
        Assert.Equal(legacyPath, Assert.Single(processLauncher.ExecutableStarts));
        Assert.Empty(processLauncher.Requests);
        Assert.Equal("Opening FiveM...", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task OpenClientCommand_WhenSelectedClientMissing_ShouldShowNotInstalledStatus()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), installLocator: new FakeClientInstallLocator());
        await vm.InitializeAsync();
        vm.SelectedOpenClient = new InstalledClientOption(GameClient.FiveMEnhanced);

        // When
        await vm.OpenClientAsync();

        // Then
        Assert.Empty(processLauncher.ExecutableStarts);
        Assert.Equal("FiveM Enhanced is not installed", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task OpenClientCommand_WhenOpenThrows_ShouldShowStartFailedStatus()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher { ThrowOnExecutableStart = true };
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), installLocator: new FakeClientInstallLocator());
        await vm.InitializeAsync();

        // When
        await vm.OpenClientAsync();

        // Then
        Assert.Equal("Launch failed", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task OpenClientCommand_WithSelection_ShouldBeExecutable()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), installLocator: new FakeClientInstallLocator());
        Assert.False(vm.OpenClientCommand.CanExecute(null));

        // When
        await vm.InitializeAsync();

        // Then
        Assert.True(vm.OpenClientCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpenClientCommand_WhileOpenInFlight_ShouldBeDisabled()
    {
        // Given
        var barrier = new TaskCompletionSource();
        var processLauncher = new FakeGameProcessLauncher { ExecutableStartBarrier = barrier.Task };
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), installLocator: new FakeClientInstallLocator());
        await vm.InitializeAsync();

        // When
        var openTask = vm.OpenClientAsync();

        // Then
        Assert.True(vm.IsBusy);
        Assert.False(vm.OpenClientCommand.CanExecute(null));

        barrier.SetResult();
        await openTask;
    }

    [Fact]
    public async Task Initialize_WhenPreferredClientInstalled_ShouldSeedSelectionToPreferred()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { PreferredClient = GameClient.FiveMEnhanced });
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            installLocator: locator,
            settings: new ConfigurationRepository(storage));

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(GameClient.FiveMEnhanced, vm.SelectedOpenClient?.Client);
        Assert.Equal("FiveM Enhanced", vm.SelectedOpenClientLabel);
    }

    [Fact]
    public async Task Initialize_WhenPreferredClientNotInstalled_ShouldFallBackToFirstInstalled()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { PreferredClient = GameClient.FiveMEnhanced });
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(GameClient.FiveM, vm.SelectedOpenClient?.Client);
    }

    [Fact]
    public async Task SetAutoLaunch_ShouldPersistImmediately()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();

        // When
        vm.AutoLaunch = true;

        // Then
        var reloaded = new ConfigurationRepository(storage).Load();
        Assert.True(reloaded.AutoLaunch);
    }

    [Fact]
    public async Task SetPreferredClientOption_ShouldPreserveOtherSettingsFields()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { LastServerAddress = "cfx.re/join/y4lg95" });
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            installLocator: locator,
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();

        // When
        vm.PreferredClientOption = vm.AvailableOpenClients[1];

        // Then
        var reloaded = new ConfigurationRepository(storage).Load();
        Assert.Equal(GameClient.FiveMEnhanced, reloaded.PreferredClient);
        Assert.Equal("cfx.re/join/y4lg95", reloaded.LastServerAddress);
    }

    [Fact]
    public async Task ConnectAsync_OnSuccessfulConnect_ShouldPersistLastServerAddress()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal(address, new ConfigurationRepository(storage).Load().LastServerAddress);
    }

    [Fact]
    public async Task ConnectAsync_WithInvalidAddress_ShouldNotPersistLastServerAddress()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { LastServerAddress = "cfx.re/join/known" });
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        vm.ServerAddress = "9 92";

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("cfx.re/join/known", new ConfigurationRepository(storage).Load().LastServerAddress);
    }

    [Fact]
    public async Task ConnectAsync_OnStartFailed_ShouldNotPersistLastServerAddress()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher { ThrowOnStart = true },
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Null(new ConfigurationRepository(storage).Load().LastServerAddress);
    }

    [Fact]
    public async Task Initialize_WithAutoLaunchAndLastServer_ShouldAutoConnect()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { AutoLaunch = true, LastServerAddress = address });
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), settings: new ConfigurationRepository(storage));

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(address, vm.ServerAddress);
        var request = Assert.Single(processLauncher.Requests);
        Assert.Equal("fivem://connect/" + address, request.AbsoluteUri);
        Assert.Equal("Launching FiveM...", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Initialize_WithoutAutoLaunch_ShouldNotAutoConnect()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { AutoLaunch = false, LastServerAddress = "cfx.re/join/y4lg95" });
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), settings: new ConfigurationRepository(storage));

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Empty(processLauncher.Requests);
        Assert.Equal("Ready", vm.StatusText);
    }

    [Fact]
    public async Task Initialize_WithAutoLaunchButNoLastServer_ShouldNotAutoConnect()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { AutoLaunch = true });
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), settings: new ConfigurationRepository(storage));

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Empty(processLauncher.Requests);
        Assert.Equal("Ready", vm.StatusText);
    }

    [Fact]
    public async Task SettingsCommand_ShouldToggleSettingsPanelOpen()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When / Then
        Assert.False(vm.IsSettingsOpen);
        vm.SettingsCommand.Execute(null);
        Assert.True(vm.IsSettingsOpen);
        vm.SettingsCommand.Execute(null);
        Assert.False(vm.IsSettingsOpen);
    }

    [Fact]
    public async Task ConnectAsync_OnClientNotInstalled_ShouldNotPersistLastServerAddress()
    {
        // Given
        const string address = "cfx.re/join/Y4LG95";
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { LastServerAddress = "cfx.re/join/known" });
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5enhanced"),
            installLocator: new FakeClientInstallLocator { Executables = [] },
            settings: new ConfigurationRepository(storage));
        vm.ServerAddress = address;

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("FiveM Enhanced is not installed", vm.StatusText);
        Assert.Equal("cfx.re/join/known", new ConfigurationRepository(storage).Load().LastServerAddress);
    }

    [Fact]
    public async Task ToggleDevModeCommand_ShouldFlipIsDevMode()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When / Then
        Assert.False(vm.IsDevMode);
        vm.ToggleDevModeCommand.Execute(null);
        Assert.True(vm.IsDevMode);
        vm.ToggleDevModeCommand.Execute(null);
        Assert.False(vm.IsDevMode);
    }

    [Fact]
    public async Task SetDevGameBuildAndPureMode_ShouldPersistThroughSettingsRepository()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();

        // When
        vm.DevGameBuild = "3095";
        vm.DevPureMode = 2;

        // Then
        var reloaded = new ConfigurationRepository(storage).Load();
        Assert.Equal(3095, reloaded.DevGameBuild);
        Assert.Equal(2, reloaded.DevPureMode);
    }

    [Fact]
    public async Task DevLaunchCommand_ShouldOpenLegacyWithPersistedFlags()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { DevGameBuild = 3095, DevPureMode = 1 });
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();

        // When
        await vm.DevLaunchAsync(false);

        // Then
        var start = Assert.Single(processLauncher.ExecutableArgsStarts);
        Assert.Equal(["-b3095", "-pure_1"], start.Args);
        Assert.Equal("Opening FiveM...", vm.StatusText);
        Assert.False(vm.IsBusy);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task DevLaunchCommand_WithSecondClientParameter_ShouldIncludeCl2()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));
        await vm.InitializeAsync();

        // When
        await vm.DevLaunchAsync(true);

        // Then
        Assert.Equal(["-cl2"], Assert.Single(processLauncher.ExecutableArgsStarts).Args);
    }

    [Fact]
    public async Task DevLaunchCommand_WithBoolParam_ShouldPassSecondClient()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));
        await vm.InitializeAsync();

        // When
        vm.DevLaunchCommand.Execute(true);

        // Then
        Assert.Equal(["-cl2"], Assert.Single(processLauncher.ExecutableArgsStarts).Args);
    }

    [Fact]
    public async Task DevLaunchCommand_WithNullParam_ShouldNotIncludeCl2()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"));
        await vm.InitializeAsync();

        // When
        vm.DevLaunchCommand.Execute(null);

        // Then
        Assert.Empty(Assert.Single(processLauncher.ExecutableArgsStarts).Args);
    }

    [Fact]
    public async Task Initialize_WithRedMInstalled_ShouldListRedMInDropdown()
    {
        // Given
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.RedM] = @"C:\RedM\RedM.app\RedM.exe";
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), installLocator: locator);

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Contains(vm.AvailableOpenClients, c => c.Client == GameClient.RedM && c.DisplayName == "RedM");
    }

    [Fact]
    public async Task OpenClientCommand_WhenRedMSelected_ShouldStartRedMExecutable()
    {
        // Given
        const string redmPath = @"C:\RedM\RedM.app\RedM.exe";
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.RedM] = redmPath;
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(processLauncher, CfxJson("gta5"), installLocator: locator);
        await vm.InitializeAsync();
        vm.SelectedOpenClient = vm.AvailableOpenClients.Single(c => c.Client == GameClient.RedM);

        // When
        await vm.OpenClientAsync();

        // Then
        Assert.Equal(redmPath, Assert.Single(processLauncher.ExecutableStarts));
        Assert.Equal("Opening RedM...", vm.StatusText);
    }

    [Fact]
    public async Task Initialize_WithRedMPreferredAndInstalled_ShouldSeedSelectionToRedM()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { PreferredClient = GameClient.RedM });
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.RedM] = @"C:\RedM\RedM.app\RedM.exe";
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            installLocator: locator,
            settings: new ConfigurationRepository(storage));

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(GameClient.RedM, vm.SelectedOpenClient?.Client);
        Assert.Equal("RedM", vm.SelectedOpenClientLabel);
    }

    [Fact]
    public async Task SelectingOpenClient_ShouldUpdateLabel()
    {
        // Given
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), installLocator: locator);
        await vm.InitializeAsync();

        // When
        vm.SelectedOpenClient = vm.AvailableOpenClients[1];

        // Then
        Assert.Equal("FiveM Enhanced", vm.SelectedOpenClientLabel);
    }

    [Fact]
    public async Task ToggleDevClientCommand_ShouldUpdateButtonText()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When / Then
        Assert.Equal("CLIENT: FiveM", vm.DevClientButtonText);
        vm.ToggleDevClientCommand.Execute(null);
        Assert.Equal("CLIENT: RedM", vm.DevClientButtonText);
    }

    [Fact]
    public async Task ToggleDevClientCommand_ShouldSwitchDevTarget()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When / Then
        Assert.Equal(GameClient.FiveM, vm.DevClient);
        vm.ToggleDevClientCommand.Execute(null);
        Assert.Equal(GameClient.RedM, vm.DevClient);
        vm.ToggleDevClientCommand.Execute(null);
        Assert.Equal(GameClient.FiveM, vm.DevClient);
    }

    [Fact]
    public async Task DevLaunchCommand_WhenRedMSelected_ShouldLaunchRedMExecutableWithFlags()
    {
        // Given
        const string redmPath = @"C:\RedM\RedM.app\RedM.exe";
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { DevGameBuild = 3116 });
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.RedM] = redmPath;
        var processLauncher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(
            processLauncher,
            CfxJson("gta5"),
            installLocator: locator,
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();
        vm.ToggleDevClientCommand.Execute(null);

        // When
        await vm.DevLaunchAsync(false);

        // Then
        var start = Assert.Single(processLauncher.ExecutableArgsStarts);
        Assert.Equal(redmPath, start.Path);
        Assert.Equal(["-b3116"], start.Args);
        Assert.Equal("Opening RedM...", vm.StatusText);
    }

    [Fact]
    public async Task DevLaunchCommand_SwitchingToRedM_ShouldNotPersist()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();

        // When
        vm.ToggleDevClientCommand.Execute(null);

        // Then
        Assert.Equal(GameClient.FiveM, new ConfigurationRepository(storage).Load().PreferredClient);
    }

    [Fact]
    public async Task SelectOpenClientCommand_ShouldChangeSelectionAndLabel()
    {
        // Given
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), installLocator: locator);
        await vm.InitializeAsync();

        // When
        vm.SelectOpenClientCommand.Execute(vm.AvailableOpenClients[1]);

        // Then
        Assert.Equal(GameClient.FiveMEnhanced, vm.SelectedOpenClient?.Client);
        Assert.Equal("FiveM Enhanced", vm.SelectedOpenClientLabel);
    }

    [Fact]
    public async Task SelectOpenClientCommand_WithNonOptionParameter_ShouldKeepSelection()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), installLocator: new FakeClientInstallLocator());
        await vm.InitializeAsync();

        // When
        vm.SelectOpenClientCommand.Execute("FiveM Enhanced");

        // Then
        Assert.Equal(GameClient.FiveM, vm.SelectedOpenClient?.Client);
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
        ExternalAppPreparer? preparer = null,
        IClientInstallLocator? installLocator = null,
        ConfigurationRepository? settings = null)
    {
        var resolver = new ServerResolver(
            new CfxService(
                new HttpClient(
                    new FakeHttpMessageHandler(HttpStatusCode.OK, cfxJson))),
            new ServerCatalog(new HttpClient(new FakeHttpMessageHandler(true))),
            new ServerRequirementsResolver(),
            new FakeDnsResolver());

        installLocator ??= new FakeClientInstallLocator();

        var launcher = new GameLauncher(processLauncher, new FakeCitizenFxPreparer(), installLocator);

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
            preparerValue,
            installLocator,
            settings ?? new ConfigurationRepository(new InMemorySettingsStorage()));
    }
}