using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Localization;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// Every store write the view model makes must survive IOException/UnauthorizedAccessException
// (locked or read-only data folder) with a status line, leaving the UI equal to the store.
public partial class MainViewModelTests
{
    private const string SaveFailed = "Could not save changes";

    public static TheoryData<Exception> WriteFailures => new()
    {
        new IOException("locked"),
        new UnauthorizedAccessException("read-only"),
        new StoreUnavailableException("servers.json")
    };

    private static InMemoryServerRepository TwoSavedServers()
    {
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("First", "cfx.re/join/abc123"));
        repository.Add(SavedServer.Create("Second", "cfx.re/join/def456"));
        return repository;
    }

    [Theory]
    [MemberData(nameof(WriteFailures))]
    public void ConfirmDelete_WhenStoreWriteFails_ShouldKeepTheRowAndShowStatus(Exception failure)
    {
        // Given
        var repository = TwoSavedServers();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        vm.DeleteServerCommand.Execute(vm.SavedServers[0]);
        repository.WriteFailure = failure;

        // When
        vm.ConfirmCommand.Execute(null);

        // Then
        Assert.Equal(2, vm.SavedServers.Count);
        Assert.Equal(2, repository.GetAll().Count);
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Theory]
    [MemberData(nameof(WriteFailures))]
    public void Reorder_WhenStoreWriteFails_ShouldKeepTheOrderAndShowStatus(Exception failure)
    {
        // Given
        var repository = TwoSavedServers();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        repository.WriteFailure = failure;

        // When — both the gutter arrow and the drag entry point
        vm.MoveServerUpCommand.Execute(vm.SavedServers[1]);
        vm.MoveServerToIndex(vm.SavedServers[1], 0);

        // Then
        Assert.Equal(new[] { "First", "Second" }, vm.SavedServers.Select(s => s.Name));
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Theory]
    [MemberData(nameof(WriteFailures))]
    public void SaveBrowserServer_WhenStoreWriteFails_ShouldAddNoRowAndShowStatus(Exception failure)
    {
        // Given
        var repository = new InMemoryServerRepository { WriteFailure = failure };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        var item = ServerBrowserItem.FromServer(new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData { Vars = { ["sv_projectName"] = "Discovered" } }
        });

        // When
        vm.SaveBrowserServerCommand.Execute(item);

        // Then
        Assert.Empty(vm.SavedServers);
        Assert.False(item.IsSaved);
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Theory]
    [MemberData(nameof(WriteFailures))]
    public async Task SaveServerDialog_OnAdd_WhenStoreWriteFails_ShouldKeepTheDialogOpenAndShowStatus(Exception failure)
    {
        // Given
        var repository = new InMemoryServerRepository { WriteFailure = failure };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "New Server";
        vm.DialogServerAddress = "cfx.re/join/abc123";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then — the typed input survives for a retry.
        Assert.True(vm.IsServerDialogOpen);
        Assert.Equal(SaveFailed, vm.DialogError);
        Assert.Equal(SaveFailed, vm.StatusText);
        Assert.Empty(vm.SavedServers);
    }

    [Theory]
    [InlineData("cfx.re/join/abc123")]
    [InlineData("cfx.re/join/zzz999")]
    public async Task SaveServerDialog_OnEdit_WhenStoreWriteFails_ShouldKeepTheRowAndShowStatus(string editedAddress)
    {
        // Given — same address (Update) and a changed address (Replace)
        var repository = TwoSavedServers();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        await vm.InitializeAsync();
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[0]);
        vm.DialogServerName = "Renamed";
        vm.DialogServerAddress = editedAddress;
        repository.WriteFailure = new IOException("locked");

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.True(vm.IsServerDialogOpen);
        Assert.Equal(SaveFailed, vm.DialogError);
        Assert.Equal(SaveFailed, vm.StatusText);
        Assert.Equal("First", vm.SavedServers[0].Name);
        Assert.Equal("cfx.re/join/abc123", vm.SavedServers[0].Address);
    }

    [Fact]
    public void RowRequirementChange_WhenStoreWriteFails_ShouldShowStatus()
    {
        // Given
        var repository = TwoSavedServers();
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        repository.WriteFailure = new UnauthorizedAccessException("read-only");

        // When
        vm.SavedServers[0].RequiresSteam = true;

        // Then
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Fact]
    public async Task CfxIdCapture_WhenStoreDeniesTheWrite_ShouldBeSwallowed()
    {
        // Given — the background capture writes after the add succeeded.
        var inner = new InMemoryServerRepository();
        var repository = new UpdateFailingRepository(inner, new UnauthorizedAccessException("read-only"));
        var enrichment = new FakeServerEnrichmentService { ResolvedCfxId = "y4lg95" };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment);
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "New Server";
        vm.DialogServerAddress = "149.56.120.52:30120";

        // When
        vm.SaveServerDialogCommand.Execute(null);
        await vm.WaitForPendingCapturesAsync();

        // Then — the save itself completed.
        Assert.False(vm.IsServerDialogOpen);
        Assert.Equal("Server saved", vm.StatusText);
        Assert.Single(vm.SavedServers);
        Assert.Null(vm.SavedServers[0].CfxId);
    }

    public static TheoryData<string> SettingChanges => new()
    {
        nameof(MainViewModel.AutoLaunch),
        nameof(MainViewModel.WebLinks),
        nameof(MainViewModel.MinimizeToTray),
        nameof(MainViewModel.DevGameBuild),
        nameof(MainViewModel.DevPureMode),
        nameof(MainViewModel.PreferredClientOption),
        nameof(MainViewModel.SelectedLanguageOption)
    };

    private static async Task<MainViewModel> SettingsViewModel(ISettingsStorage storage)
    {
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        locator.Executables[GameClient.FiveM] = @"C:\FiveM\FiveM.app\FiveM.exe";
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            installLocator: locator,
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();
        return vm;
    }

    private static object? ReadSetting(MainViewModel vm, string setting) => setting switch
    {
        nameof(MainViewModel.AutoLaunch) => vm.AutoLaunch,
        nameof(MainViewModel.WebLinks) => vm.WebLinks,
        nameof(MainViewModel.MinimizeToTray) => vm.MinimizeToTray,
        nameof(MainViewModel.DevGameBuild) => vm.DevGameBuild,
        nameof(MainViewModel.DevPureMode) => vm.DevPureMode,
        nameof(MainViewModel.PreferredClientOption) => vm.PreferredClientOption,
        _ => vm.SelectedLanguageOption
    };

    private static void ChangeSetting(MainViewModel vm, string setting)
    {
        switch (setting)
        {
            case nameof(MainViewModel.AutoLaunch):
                vm.AutoLaunch = !vm.AutoLaunch;
                break;
            case nameof(MainViewModel.WebLinks):
                vm.WebLinks = !vm.WebLinks;
                break;
            case nameof(MainViewModel.MinimizeToTray):
                vm.MinimizeToTray = !vm.MinimizeToTray;
                break;
            case nameof(MainViewModel.DevGameBuild):
                vm.DevGameBuild = 3095;
                break;
            case nameof(MainViewModel.DevPureMode):
                vm.DevPureMode = 2;
                break;
            case nameof(MainViewModel.PreferredClientOption):
                vm.PreferredClientOption = vm.AvailableOpenClients.First(o => !ReferenceEquals(o, vm.PreferredClientOption));
                break;
            default:
                vm.SelectedLanguageOption = vm.LanguageOptions.First(o => !ReferenceEquals(o, vm.SelectedLanguageOption));
                break;
        }
    }

    [Theory]
    [MemberData(nameof(SettingChanges))]
    public async Task SettingChange_WhenSaveFails_ShouldRevertTheValueAndShowStatus(string setting)
    {
        // Given
        var storage = new ThrowingSettingsStorage();
        var vm = await SettingsViewModel(storage);
        var before = ReadSetting(vm, setting);
        storage.SaveFailure = new UnauthorizedAccessException("read-only");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        ChangeSetting(vm, setting);

        // Then — the binding snaps back to what the store still holds.
        Assert.Equal(before, ReadSetting(vm, setting));
        Assert.Contains(setting, raised);
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Fact]
    public async Task LanguageChange_WhenSaveFails_ShouldNotApplyTheLanguage()
    {
        // Given
        var storage = new ThrowingSettingsStorage();
        var localizer = TestLocalizer.English();
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage),
            localizer: localizer);
        await vm.InitializeAsync();
        storage.SaveFailure = new IOException("locked");

        // When
        vm.SelectedLanguageOption = vm.LanguageOptions.Single(o => o.Tag == "es");

        // Then
        Assert.Equal("en", localizer.Language);
        Assert.Null(vm.SelectedLanguageOption.Tag);
    }

    [Fact]
    public async Task SettingChange_WhenSettingsFileWasLockedAtStartup_ShouldLeaveItByteIdentical()
    {
        // Given — the real storage over a file locked while the view model loaded it.
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, """{"AutoLaunch":true,"LastServerAddress":"abc123"}""");
        var before = File.ReadAllBytes(tempDir.FilePath);
        MainViewModel vm;

        using (File.Open(tempDir.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            vm = await SettingsViewModel(new FileSettingsStorage(tempDir.FilePath));
        }

        // When
        vm.AutoLaunch = true;
        vm.DevGameBuild = 3095;

        // Then
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Fact]
    public async Task Delete_WhenServersFileIsLocked_ShouldLeaveItByteIdentical()
    {
        // Given — the real repository; the file is unreadable for the whole session.
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, """[{"Name":"First","Address":"abc123"}]""");
        var before = File.ReadAllBytes(tempDir.FilePath);
        using var lockHandle = File.Open(tempDir.FilePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), new FileServerRepository(tempDir.FilePath));
        await vm.InitializeAsync();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "New Server";
        vm.DialogServerAddress = "cfx.re/join/zzz999";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        lockHandle.Dispose();
        Assert.Equal(before, File.ReadAllBytes(tempDir.FilePath));
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Fact]
    public async Task Connect_WhenLastAddressSaveFails_ShouldStillReportTheLaunch()
    {
        // Given
        var storage = new ThrowingSettingsStorage { SaveFailure = new IOException("locked") };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage));
        vm.ServerAddress = "cfx.re/join/abc123";

        // When
        await vm.ConnectAsync();

        // Then
        Assert.Equal("Launching FiveM...", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task InitializeAsync_WhenStartupHitsAPermissionFault_ShouldShowStartupFailed()
    {
        // Given
        var installLocator = new FakeClientInstallLocator { IsInstalledFailure = new UnauthorizedAccessException() };
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), installLocator: installLocator);

        // When
        await vm.InitializeAsync();

        // Then
        Assert.False(vm.IsBusy);
        Assert.Equal("Startup failed", vm.StatusText);
    }

    [Fact]
    public void Construction_WhenBothStoresDenyTheFirstRead_ShouldStartWithDefaults()
    {
        // Given — a read-only/ACL-denied data folder
        using var tempDir = new TempSettingsDirectory();
        Directory.CreateDirectory(tempDir.DirectoryPath);
        File.WriteAllText(tempDir.FilePath, "[]");
        Func<string, string> denied = _ => throw new UnauthorizedAccessException();

        // When
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            new FileServerRepository(tempDir.FilePath, denied),
            settings: new ConfigurationRepository(new FileSettingsStorage(tempDir.FilePath, denied)));

        // Then
        Assert.Empty(vm.SavedServers);
        Assert.False(vm.AutoLaunch);
    }

    [Fact]
    public void ReportUnexpectedError_ShouldShowTheGenericStatus()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // When
        vm.ReportUnexpectedError();

        // Then
        Assert.Equal("Something went wrong", vm.StatusText);
    }

    internal sealed class ThrowingSettingsStorage : ISettingsStorage
    {
        private LauncherSettings? _settings;

        public Exception? SaveFailure { get; set; }

        public void Save(LauncherSettings settings)
        {
            if (SaveFailure is { } failure)
            {
                throw failure;
            }

            _settings = settings;
        }

        public LauncherSettings? Load() => _settings;
    }

    private sealed class UpdateFailingRepository(IServerRepository inner, Exception failure) : IServerRepository
    {
        public IReadOnlyList<SavedServer> GetAll() => inner.GetAll();

        public SavedServer? FindByAddress(string address) => inner.FindByAddress(address);

        public SavedServer? FindByCfxId(string cfxId) => inner.FindByCfxId(cfxId);

        public void Add(SavedServer savedServer) => inner.Add(savedServer);

        public void Update(SavedServer savedServer) => throw failure;

        public void Replace(string address, SavedServer replacement) => inner.Replace(address, replacement);

        public void Remove(string address) => inner.Remove(address);

        public void Move(string address, int newIndex) => inner.Move(address, newIndex);
    }
}
