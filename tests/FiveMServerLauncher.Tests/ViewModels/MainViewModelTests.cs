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
    public void Ctor_WhenSavedServerHasCfxId_ShouldExposeItOnRow()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "192.168.1.10:30120", cfxId: "y4lg95"));

        // When
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);

        // Then
        Assert.Equal("y4lg95", Assert.Single(vm.SavedServers).CfxId);
    }

    [Fact]
    public async Task RefreshServerInfoAsync_WhenOnline_ShouldPopulatePlayersAndIcon()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence>
            {
                ["y4lg95"] = new ServerPresence(true, 12, 64, GameClient.FiveM)
            },
            Icon = [1, 2, 3]
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);

        // When
        await vm.RefreshServerInfoAsync();

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.True(item.Online);
        Assert.Equal(12, item.Players);
        Assert.Equal(64, item.MaxPlayers);
        Assert.Equal("12/64", item.StatusLabel);
        Assert.Equal(GameClient.FiveM, item.Game);
        Assert.Equal("FiveM", item.GameTagLabel);
        Assert.True(item.HasIcon);
    }

    [Fact]
    public async Task RefreshServerInfoAsync_WhenCfxIdNotInCatalog_ShouldShowOfflineStatus()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var enrichment = new FakeServerEnrichmentService { Presence = new Dictionary<string, ServerPresence>() };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);

        // When
        await vm.RefreshServerInfoAsync();

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.False(item.Online);
        Assert.Equal("OFFLINE", item.StatusLabel);
        Assert.False(item.HasIcon);
    }

    [Fact]
    public async Task RefreshServerInfoAsync_WhenNoCfxId_ShouldLeaveRowUnenriched()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "not-published.example.com:30120"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence> { ["y4lg95"] = new ServerPresence(true, 1, 32, GameClient.FiveM) }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);

        // When
        await vm.RefreshServerInfoAsync();

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.False(item.Online);
        Assert.Equal("UNRESOLVED", item.StatusLabel);
        Assert.False(item.HasIcon);
        Assert.Equal(0, enrichment.IconCalls);
    }

    [Fact]
    public async Task SaveServer_WithIpPortAddress_ShouldCaptureCfxIdInBackgroundAndPersist()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var enrichment = new FakeServerEnrichmentService { ResolvedCfxId = "y4lg95" };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Ip Server";
        vm.DialogServerAddress = "149.56.120.52:30320";

        // When
        vm.SaveServerDialogCommand.Execute(null);
        await vm.WaitForPendingCapturesAsync();

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.Equal("y4lg95", item.CfxId);
        Assert.Equal("y4lg95", Assert.Single(repository.GetAll()).CfxId);
        Assert.Equal(1, enrichment.ResolveCalls);
    }

    [Fact]
    public async Task SaveServer_WithIpPortAddress_WhenNotResolvable_ShouldKeepNullCfxId()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var enrichment = new FakeServerEnrichmentService { ResolvedCfxId = null };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Ip Server";
        vm.DialogServerAddress = "149.56.120.52:30320";

        // When
        vm.SaveServerDialogCommand.Execute(null);
        await vm.WaitForPendingCapturesAsync();

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.Null(item.CfxId);
        Assert.Null(Assert.Single(repository.GetAll()).CfxId);
    }

    [Fact]
    public void SaveServer_WithCfxJoinAddress_ShouldStoreCfxIdImmediately()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var enrichment = new FakeServerEnrichmentService();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Cfx Server";
        vm.DialogServerAddress = "cfx.re/join/y4lg95";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        var item = Assert.Single(vm.SavedServers);
        Assert.Equal("y4lg95", item.CfxId);
        Assert.Equal("y4lg95", Assert.Single(repository.GetAll()).CfxId);
        Assert.Equal(0, enrichment.ResolveCalls);
    }

    [Fact]
    public void SaveServer_WhenEditingName_ShouldPreserveCapturedCfxId()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: new FakeServerEnrichmentService());
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);

        // When
        vm.DialogServerName = "Renamed";
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal("y4lg95", Assert.Single(repository.GetAll()).CfxId);
        Assert.Equal("y4lg95", Assert.Single(vm.SavedServers).CfxId);
    }

    [Fact]
    public void SaveServer_WhenEditingIpPortName_ShouldPreserveCapturedCfxId()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Ip Server", "149.56.120.52:30320", cfxId: "y4lg95"));
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: new FakeServerEnrichmentService());
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);
        vm.DialogServerName = "Renamed";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal("y4lg95", Assert.Single(repository.GetAll()).CfxId);
        Assert.Equal("y4lg95", Assert.Single(vm.SavedServers).CfxId);
    }

    [Fact]
    public async Task RunEnrichmentLoopAsync_ShouldRefreshThenWaitUntilCancelled()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence>
            {
                ["y4lg95"] = new ServerPresence(true, 4, 32, GameClient.RedM)
            }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);
        using var cts = new CancellationTokenSource();
        var loop = vm.RunEnrichmentLoopAsync(
            cancellationToken: cts.Token,
            cadence: TimeSpan.FromMinutes(1),
            delay: async (_, _) => await Task.Yield());

        // When
        while (enrichment.RefreshCalls < 3)
        {
            await Task.Yield();
        }

        cts.Cancel();
        await loop;

        // Then
        Assert.True(enrichment.RefreshCalls >= 3);
        Assert.Equal("4/32", Assert.Single(vm.SavedServers).StatusLabel);
    }

    [Fact]
    public async Task RunEnrichmentLoopAsync_WhenRefreshThrows_ShouldKeepLooping()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var enrichment = new FakeServerEnrichmentService
        {
            ThrowOnRefreshCount = 1,
            Presence = new Dictionary<string, ServerPresence>
            {
                ["y4lg95"] = new ServerPresence(true, 4, 32, GameClient.FiveM)
            }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: enrichment);
        using var cts = new CancellationTokenSource();
        var loop = vm.RunEnrichmentLoopAsync(
            cancellationToken: cts.Token,
            cadence: TimeSpan.FromMinutes(1),
            delay: async (_, _) => await Task.Yield());

        // When
        var spins = 0;
        while (enrichment.RefreshCalls < 3 && spins++ < 100_000)
        {
            await Task.Yield();
        }

        cts.Cancel();
        await loop;

        // Then
        Assert.True(enrichment.RefreshCalls >= 3);
    }

    [Fact]
    public void SaveServer_WhenValid_ShouldAddToCollectionAndRepository()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository: repository);
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "My Server";
        vm.DialogServerAddress = "cfx.re/join/abc123";

        // When
        vm.SaveServerDialogCommand.Execute(null);

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
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "My Server";
        vm.DialogServerAddress = "9 92";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal("Invalid name or address", vm.DialogError);
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
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Second";
        vm.DialogServerAddress = "abc123";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal("Server already saved", vm.DialogError);
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
    public async Task ToggleDevModeCommand_WhenSettingsOpen_ShouldCloseSettings()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();
        vm.SettingsCommand.Execute(null);

        // When
        vm.ToggleDevModeCommand.Execute(null);

        // Then
        Assert.True(vm.IsDevMode);
        Assert.False(vm.IsSettingsOpen);
    }

    [Fact]
    public async Task SettingsCommand_WhenDevModeOpen_ShouldCloseDevMode()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();
        vm.ToggleDevModeCommand.Execute(null);

        // When
        vm.SettingsCommand.Execute(null);

        // Then
        Assert.False(vm.IsDevMode);
        Assert.True(vm.IsSettingsOpen);
    }

    [Fact]
    public async Task OpenAddServerDialogCommand_ShouldOpenEmptyDialog()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When
        vm.OpenAddServerDialogCommand.Execute(null);

        // Then
        Assert.True(vm.IsServerDialogOpen);
        Assert.Null(vm.EditingServer);
        Assert.Equal(string.Empty, vm.DialogServerName);
        Assert.Equal(string.Empty, vm.DialogServerAddress);
        Assert.False(vm.DialogRequiresSteam);
        Assert.False(vm.DialogRequiresDiscord);
    }

    [Fact]
    public async Task OpenEditServerDialogCommand_ShouldPrefillFromRow()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95", requiresSteam: true));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();

        // When
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);

        // Then
        Assert.True(vm.IsServerDialogOpen);
        Assert.Equal(vm.SavedServers[0], vm.EditingServer);
        Assert.Equal("My Server", vm.DialogServerName);
        Assert.Equal("cfx.re/join/y4lg95", vm.DialogServerAddress);
        Assert.True(vm.DialogRequiresSteam);
    }

    [Fact]
    public async Task SaveServerDialogCommand_OnAdd_ShouldPersistAndCloseDialog()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "New Server";
        vm.DialogServerAddress = "cfx.re/join/abc123";
        vm.DialogRequiresDiscord = true;

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.False(vm.IsServerDialogOpen);
        Assert.Single(vm.SavedServers);
        Assert.Equal("New Server", vm.SavedServers[0].Name);
        Assert.Equal("cfx.re/join/abc123", vm.SavedServers[0].Address);
        Assert.True(vm.SavedServers[0].RequiresDiscord);
        Assert.Equal("Server saved", vm.StatusText);
    }

    [Fact]
    public async Task SaveServerDialogCommand_OnAdd_ShouldForceRefreshAndEnrichNewRow()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var enrichment = new FakeServerEnrichmentService
        {
            ResolvedCfxId = "abc123",
            Presence = new Dictionary<string, ServerPresence>
            {
                ["abc123"] = new ServerPresence(true, 12, 48, GameClient.FiveM)
            },
            Icon = [1, 2, 3]
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment);
        await vm.InitializeAsync();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "New Server";
        vm.DialogServerAddress = "149.56.120.52:30120";
        var refreshCallsBefore = enrichment.RefreshCalls;

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal(1, enrichment.ForcedRefreshCalls);
        Assert.True(enrichment.RefreshCalls > refreshCallsBefore);
        Assert.Equal("12/48", vm.SavedServers[0].StatusLabel);
        Assert.NotNull(vm.SavedServers[0].Icon);
        Assert.Equal("Server saved", vm.StatusText);
    }

    [Fact]
    public async Task SaveServerDialogCommand_OnEdit_ShouldForceRefresh()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Old Name", "cfx.re/join/keep"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence>
            {
                ["keep"] = new ServerPresence(true, 5, 32, GameClient.FiveM)
            }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment);
        await vm.InitializeAsync();
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);

        // When
        vm.DialogServerName = "New Name";
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal(1, enrichment.ForcedRefreshCalls);
        Assert.Equal("Server saved", vm.StatusText);
    }

    [Fact]
    public async Task SaveServerDialogCommand_WhenForcedRefreshFails_ShouldStillSaveAndClose()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var enrichment = new FakeServerEnrichmentService { ThrowOnRefreshCount = 1 };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment);
        await vm.InitializeAsync();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "New Server";
        vm.DialogServerAddress = "cfx.re/join/abc123";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.False(vm.IsServerDialogOpen);
        Assert.Single(vm.SavedServers);
        Assert.Equal("Server saved", vm.StatusText);
    }

    [Fact]
    public async Task SaveServerDialogCommand_OnEdit_ShouldUpdateSameAddress()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Old Name", "cfx.re/join/keep"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);

        // When
        vm.DialogServerName = "New Name";
        vm.DialogRequiresSteam = true;
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Single(vm.SavedServers);
        Assert.Equal("New Name", vm.SavedServers[0].Name);
        Assert.True(vm.SavedServers[0].RequiresSteam);
        Assert.False(vm.IsServerDialogOpen);
    }

    [Fact]
    public async Task SaveServerDialogCommand_OnEditAddressChange_ShouldRemoveOldAndAddNew()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Move Me", "cfx.re/join/old"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);

        // When
        vm.DialogServerAddress = "cfx.re/join/new";
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Single(vm.SavedServers);
        Assert.Equal("cfx.re/join/new", vm.SavedServers[0].Address);
        Assert.Null(repository.FindByAddress("cfx.re/join/old"));
        Assert.NotNull(repository.FindByAddress("cfx.re/join/new"));
    }

    [Fact]
    public async Task SaveServerDialogCommand_WithInvalidInput_ShouldNotCloseDialog()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "  ";
        vm.DialogServerAddress = "9 92";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.True(vm.IsServerDialogOpen);
        Assert.Equal("Invalid name or address", vm.DialogError);
    }

    [Fact]
    public async Task CancelServerDialogCommand_ShouldCloseWithoutSaving()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Touched";

        // When
        vm.CancelServerDialogCommand.Execute(null);

        // Then
        Assert.False(vm.IsServerDialogOpen);
        Assert.Empty(vm.SavedServers);
    }

    [Fact]
    public async Task ServerDialogTitle_OnAdd_ShouldBeNewServer()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When
        vm.OpenAddServerDialogCommand.Execute(null);

        // Then
        Assert.Equal("NEW SERVER", vm.ServerDialogTitle);
    }

    [Fact]
    public async Task ServerDialogTitle_OnEdit_ShouldBeEditServer()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();

        // When
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);

        // Then
        Assert.Equal("EDIT SERVER", vm.ServerDialogTitle);
    }

    [Fact]
    public async Task SaveServerDialogCommand_OnEditSameAddressCaseInsensitive_ShouldUpdateInPlace()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Old Name", "cfx.re/join/keep"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);

        // When
        vm.DialogServerName = "New Name";
        vm.DialogServerAddress = "CFX.RE/JOIN/KEEP";
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Single(vm.SavedServers);
        Assert.Equal("New Name", vm.SavedServers[0].Name);
        Assert.Equal("CFX.RE/JOIN/KEEP", vm.SavedServers[0].Address);
        Assert.False(vm.IsServerDialogOpen);
    }

    [Fact]
    public async Task SaveServerDialogCommand_OnDuplicateEditAddress_ShouldNotCloseDialog()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Keep Me", "cfx.re/join/keep"));
        repository.Add(SavedServer.Create("Edit Me", "cfx.re/join/edit"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[1]);

        // When
        vm.DialogServerAddress = "cfx.re/join/keep";
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.True(vm.IsServerDialogOpen);
        Assert.Equal("Server already saved", vm.DialogError);
        Assert.Equal(2, vm.SavedServers.Count);
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

    [Fact]
    public async Task RefreshCfxStatusAsync_WhenStatusesReturned_ShouldApplyToAllThreeProperties()
    {
        // Given
        var statusService = new FakeCfxStatusService
        {
            Statuses = new Dictionary<GameClient, CfxStatus>
            {
                [GameClient.FiveM] = CfxStatus.Degraded,
                [GameClient.FiveMEnhanced] = CfxStatus.MajorOutage,
                [GameClient.RedM] = CfxStatus.Operational
            }
        };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), cfxStatus: statusService);

        // When
        await vm.RefreshCfxStatusAsync();

        // Then
        Assert.Equal(CfxStatus.Degraded, vm.FiveMStatus.Status);
        Assert.Equal("DEGRADED", vm.FiveMStatus.StatusLabel);
        Assert.Equal(CfxStatus.MajorOutage, vm.FiveMEnhancedStatus.Status);
        Assert.Equal("OUTAGE", vm.FiveMEnhancedStatus.StatusLabel);
        Assert.Equal(CfxStatus.Operational, vm.RedMStatus.Status);
        Assert.Equal("OPERATIONAL", vm.RedMStatus.StatusLabel);
    }

    [Fact]
    public async Task RefreshCfxStatusAsync_WhenStatusNull_ShouldKeepLastKnownStatus()
    {
        // Given
        var statusService = new FakeCfxStatusService
        {
            Statuses = new Dictionary<GameClient, CfxStatus>
            {
                [GameClient.FiveM] = CfxStatus.Operational,
                [GameClient.FiveMEnhanced] = CfxStatus.Operational,
                [GameClient.RedM] = CfxStatus.Operational
            }
        };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), cfxStatus: statusService);
        await vm.RefreshCfxStatusAsync();
        statusService.Statuses = null;

        // When
        await vm.RefreshCfxStatusAsync();

        // Then
        Assert.Equal(CfxStatus.Operational, vm.FiveMStatus.Status);
        Assert.Equal("OPERATIONAL", vm.FiveMStatus.StatusLabel);
    }

    [Fact]
    public async Task RefreshCfxStatusAsync_WhenFirstFetchFails_ShouldShowUnknown()
    {
        // Given
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            cfxStatus: new FakeCfxStatusService());

        // When
        await vm.RefreshCfxStatusAsync();

        // Then
        Assert.Equal(CfxStatus.Unknown, vm.FiveMStatus.Status);
        Assert.Equal("UNKNOWN", vm.FiveMStatus.StatusLabel);
        Assert.Equal(CfxStatus.Unknown, vm.FiveMEnhancedStatus.Status);
        Assert.Equal(CfxStatus.Unknown, vm.RedMStatus.Status);
    }

    [Fact]
    public async Task RunEnrichmentLoopAsync_ShouldRefreshCfxStatusEachCycle()
    {
        // Given
        var statusService = new FakeCfxStatusService
        {
            Statuses = new Dictionary<GameClient, CfxStatus>
            {
                [GameClient.FiveM] = CfxStatus.Operational,
                [GameClient.FiveMEnhanced] = CfxStatus.Operational,
                [GameClient.RedM] = CfxStatus.MajorOutage
            }
        };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), cfxStatus: statusService);
        using var cts = new CancellationTokenSource();
        var loop = vm.RunEnrichmentLoopAsync(
            cancellationToken: cts.Token,
            cadence: TimeSpan.FromMinutes(1),
            delay: async (_, _) => await Task.Yield());

        // When
        var spins = 0;
        while (statusService.GetCalls < 3 && spins++ < 100_000)
        {
            await Task.Yield();
        }

        cts.Cancel();
        await loop;

        // Then
        Assert.True(statusService.GetCalls >= 3);
        Assert.Equal(CfxStatus.MajorOutage, vm.RedMStatus.Status);
    }

    [Fact]
    public async Task RunEnrichmentLoopAsync_WhenCfxStatusServiceThrows_ShouldKeepLooping()
    {
        // Given
        var statusService = new FakeCfxStatusService
        {
            ThrowOnGetCount = 1,
            Statuses = new Dictionary<GameClient, CfxStatus>
            {
                [GameClient.FiveM] = CfxStatus.Operational,
                [GameClient.FiveMEnhanced] = CfxStatus.Operational,
                [GameClient.RedM] = CfxStatus.Operational
            }
        };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), cfxStatus: statusService);
        using var cts = new CancellationTokenSource();
        var loop = vm.RunEnrichmentLoopAsync(
            cancellationToken: cts.Token,
            cadence: TimeSpan.FromMinutes(1),
            delay: async (_, _) => await Task.Yield());

        // When
        var spins = 0;
        while (statusService.GetCalls < 3 && spins++ < 100_000)
        {
            await Task.Yield();
        }

        cts.Cancel();
        await loop;

        // Then
        Assert.True(statusService.GetCalls >= 3);
        Assert.Equal(CfxStatus.Operational, vm.RedMStatus.Status);
    }

    [Fact]
    public void CfxStatuses_ShouldExposeFixedOrderedTrio()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // When / Then
        Assert.Equal(
            [GameClient.FiveM, GameClient.FiveMEnhanced, GameClient.RedM],
            vm.CfxStatuses.Select(i => i.Client));
    }

    [Fact]
    public async Task InitializeAsync_ShouldRefreshCfxStatusOnceAtStartup()
    {
        // Given
        var statusService = new FakeCfxStatusService
        {
            Statuses = new Dictionary<GameClient, CfxStatus>
            {
                [GameClient.FiveM] = CfxStatus.Operational,
                [GameClient.FiveMEnhanced] = CfxStatus.Degraded,
                [GameClient.RedM] = CfxStatus.Operational
            }
        };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), cfxStatus: statusService);

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(1, statusService.GetCalls);
        Assert.Equal(CfxStatus.Degraded, vm.FiveMEnhancedStatus.Status);
        Assert.Equal("DEGRADED", vm.FiveMEnhancedStatus.StatusLabel);
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

    [Fact]
    public async Task RefreshServersCommand_ShouldForceRefreshAndUpdateRows()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence>
            {
                ["y4lg95"] = new ServerPresence(true, 9, 32, GameClient.FiveM)
            }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment);
        await vm.InitializeAsync();

        // When
        vm.RefreshServersCommand.Execute(null);

        // Then
        Assert.Equal(1, enrichment.ForcedRefreshCalls);
        Assert.Equal("9/32", vm.SavedServers[0].StatusLabel);
        Assert.Equal("Ready", vm.StatusText);
    }

    [Fact]
    public async Task RefreshServersCommand_WhenOutage_ShouldLeaveRowsUntouched()
    {
        // Given — presence null means outage; row keeps its prior state
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("My Server", "cfx.re/join/y4lg95"));
        var enrichment = new FakeServerEnrichmentService { Presence = null };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment);
        await vm.InitializeAsync();
        vm.SavedServers[0].ApplyPresence(new ServerPresence(true, 7, 32, GameClient.FiveM));

        // When
        vm.RefreshServersCommand.Execute(null);

        // Then
        Assert.Equal("7/32", vm.SavedServers[0].StatusLabel);
    }

    [Fact]
    public async Task RefreshServersCommand_WhileRunning_ShouldBeDisabled()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var enrichment = new FakeServerEnrichmentService();
        var gate = new TaskCompletionSource();
        enrichment.RefreshDelay = gate.Task;
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository,
            enrichment: enrichment, refreshCooldown: TimeSpan.Zero);
        await vm.InitializeAsync();

        // When
        vm.RefreshServersCommand.Execute(null);

        // Then — mid-flight, the command refuses overlap
        Assert.False(vm.RefreshServersCommand.CanExecute(null));
        Assert.True(vm.IsRefreshingServers);

        gate.SetResult();
        await vm.WaitForPendingCapturesAsync();
        await Task.Delay(50);

        Assert.True(vm.RefreshServersCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshServersCommand_AfterRefresh_ShouldStayDisabledForCooldown()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var enrichment = new FakeServerEnrichmentService();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository,
            enrichment: enrichment, refreshCooldown: TimeSpan.FromMilliseconds(400));
        await vm.InitializeAsync();

        // When — refresh completes instantly, but the cooldown window is still active
        vm.RefreshServersCommand.Execute(null);
        await Task.Delay(150);

        // Then
        Assert.False(vm.RefreshServersCommand.CanExecute(null));

        // And re-enables once the cooldown elapses
        await Task.Delay(400);
        Assert.True(vm.RefreshServersCommand.CanExecute(null));
        Assert.False(vm.IsRefreshingServers);
    }

    [Fact]
    public async Task ServerSearchText_WhenNameFragmentTyped_ShouldFilterVisibleRowsCaseInsensitive()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Alpha Zone", "cfx.re/join/aaaaaa"));
        repository.Add(SavedServer.Create("Beta World", "cfx.re/join/bbbbbb"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();

        // When
        vm.ServerSearchText = "ALPHA";

        // Then
        var visible = vm.SavedServersView.Cast<SavedServerItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Alpha Zone", visible[0].Name);
        Assert.Equal(2, vm.SavedServers.Count);
    }

    [Fact]
    public async Task ServerSearchText_WhenAddressFragmentTyped_ShouldFilterVisibleRows()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Alpha Zone", "149.56.120.52:30120"));
        repository.Add(SavedServer.Create("Beta World", "10.0.0.1:30120"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();

        // When
        vm.ServerSearchText = "149.56";

        // Then
        var visible = vm.SavedServersView.Cast<SavedServerItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Alpha Zone", visible[0].Name);
    }

    [Fact]
    public async Task ServerSearchText_WhenCleared_ShouldRestoreAllRows()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Alpha Zone", "cfx.re/join/aaaaaa"));
        repository.Add(SavedServer.Create("Beta World", "cfx.re/join/bbbbbb"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.ServerSearchText = "alpha";

        // When
        vm.ServerSearchText = "";

        // Then
        Assert.Equal(2, vm.SavedServersView.Cast<SavedServerItem>().Count());
    }

    [Fact]
    public async Task ServerSearchText_WhenRowFilteredOut_ShouldStillReceiveEnrichment()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Alpha Zone", "cfx.re/join/aaaaaa"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence>
            {
                ["aaaaaa"] = new ServerPresence(true, 4, 32, GameClient.FiveM)
            }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment);
        await vm.InitializeAsync();
        vm.ServerSearchText = "nothing-matches";

        // When
        await vm.RefreshServerInfoAsync();

        // Then — hidden row still updates
        Assert.Equal("4/32", vm.SavedServers[0].StatusLabel);
        Assert.Empty(vm.SavedServersView.Cast<SavedServerItem>());
    }

    [Fact]
    public async Task SaveBrowserServerCommand_WhenNotSaved_ShouldSaveDirectlyAndMarkRow()
    {
        // Given
        var repository = new InMemoryServerRepository();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        var item = ServerBrowserItem.FromServer(new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars = { ["sv_projectName"] = "Discovered" },
                ConnectEndPoints = { "149.56.120.52:30120" }
            }
        });

        // When
        vm.SaveBrowserServerCommand.Execute(item);
        await vm.WaitForPendingCapturesAsync();

        // Then — saved straight to the list, no dialog involved
        Assert.False(vm.IsServerDialogOpen);
        Assert.Single(vm.SavedServers);
        Assert.Equal("Discovered", vm.SavedServers[0].Name);
        Assert.Equal("149.56.120.52:30120", vm.SavedServers[0].Address);
        Assert.Equal("y4lg95", repository.FindByAddress("149.56.120.52:30120")?.CfxId);
        Assert.True(item.IsSaved);
    }

    [Fact]
    public async Task SaveBrowserServerCommand_WhenAlreadySaved_ShouldDoNothing()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Mine", "149.56.120.52:30120"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        var item = ServerBrowserItem.FromServer(new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars = { ["sv_projectName"] = "Discovered" },
                ConnectEndPoints = { "149.56.120.52:30120" }
            }
        });
        item.SetSaved(true);

        // When
        vm.SaveBrowserServerCommand.Execute(item);

        // Then
        Assert.Single(vm.SavedServers);
        Assert.False(vm.IsServerDialogOpen);
    }

    [Fact]
    public async Task ConnectFromBrowserAsync_ShouldConnectByCfxIdCloseBrowserAndPersist()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";
        var storage = new InMemorySettingsStorage();
        var launcher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(
            launcher, CfxJson("gta5"), settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();
        vm.OpenServerBrowserCommand.Execute(null);
        await Task.Delay(100);
        Assert.True(vm.IsServerBrowserOpen);
        var item = ServerBrowserItem.FromServer(new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30120" } }
        });

        // When
        await vm.ConnectFromBrowserAsync(item);

        // Then
        Assert.False(vm.IsServerBrowserOpen);
        Assert.Equal(address, vm.ServerAddress);
        Assert.Single(launcher.Requests);
        Assert.Equal(
            address,
            new ConfigurationRepository(storage).Load().LastServerAddress);
    }

    [Fact]
    public async Task ConnectBrowserServerCommand_ShouldDelegateToConnectPipeline()
    {
        // Given
        var launcher = new FakeGameProcessLauncher();
        var vm = CreateViewModel(launcher, CfxJson("gta5"));
        await vm.InitializeAsync();
        var item = ServerBrowserItem.FromServer(new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30120" } }
        });

        // When
        vm.ConnectBrowserServerCommand.Execute(item);
        await Task.Delay(100);

        // Then
        Assert.Single(launcher.Requests);
    }

    private static MainViewModel CreateViewModel(
        IGameProcessLauncher processLauncher,
        string cfxJson,
        IServerRepository? repository = null,
        IRequirementReadiness? readiness = null,
        FakeExternalAppStarter? starter = null,
        ExternalAppPreparer? preparer = null,
        IClientInstallLocator? installLocator = null,
        ConfigurationRepository? settings = null,
        IServerEnrichmentService? enrichment = null,
        ICfxStatusService? cfxStatus = null,
        TimeSpan? refreshCooldown = null,
        ServerBrowserViewModel? browser = null)
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
            settings ?? new ConfigurationRepository(new InMemorySettingsStorage()),
            enrichment ?? new FakeServerEnrichmentService(),
            cfxStatus ?? new FakeCfxStatusService(),
            browser ?? new ServerBrowserViewModel(
                new ServerCatalog(new HttpClient(new FakeHttpMessageHandler(true))),
                enrichment ?? new FakeServerEnrichmentService(),
                repository ?? new InMemoryServerRepository()),
            refreshCooldown: refreshCooldown);
    }
}