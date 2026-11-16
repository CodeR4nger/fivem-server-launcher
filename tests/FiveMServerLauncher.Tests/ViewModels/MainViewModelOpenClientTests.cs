using System.ComponentModel;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.ViewModels;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// The bottom bar's open-client button and the Settings Preferred client control are two
// editors of ONE state: PreferredClientOption. The button opens it; its dropdown chooses it.
public partial class MainViewModelTests
{
    private const string RedMExecutablePath = @"C:\RedM\RedM.app\RedM.exe";

    private static FakeClientInstallLocator AllClientsInstalled()
    {
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;
        locator.Executables[GameClient.RedM] = RedMExecutablePath;
        return locator;
    }

    private static async Task<MainViewModel> OpenClientViewModel(
        ISettingsStorage storage,
        FakeClientInstallLocator? locator = null,
        FakeGameProcessLauncher? processLauncher = null)
    {
        var vm = CreateViewModel(
            processLauncher ?? new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            installLocator: locator ?? AllClientsInstalled(),
            settings: new ConfigurationRepository(storage));
        await vm.InitializeAsync();
        return vm;
    }

    [Fact]
    public async Task Initialize_WithLegacyAndEnhancedInstalled_ShouldListBothAndPreferTheFirst()
    {
        // Given
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.FiveMEnhanced] = EnhancedExecutablePath;

        // When
        var vm = await OpenClientViewModel(new InMemorySettingsStorage(), locator);

        // Then
        Assert.Equal(["FiveM", "FiveM Enhanced"], vm.AvailableOpenClients.Select(c => c.DisplayName));
        Assert.Equal(GameClient.FiveM, vm.PreferredClientOption?.Client);
        Assert.Equal("Open FiveM", vm.OpenClientLabel);
        Assert.True(vm.HasInstalledClients);
    }

    [Fact]
    public async Task Initialize_WithRedMInstalled_ShouldOfferRedM()
    {
        // Given
        var locator = new FakeClientInstallLocator();
        locator.Executables[GameClient.RedM] = RedMExecutablePath;

        // When
        var vm = await OpenClientViewModel(new InMemorySettingsStorage(), locator);

        // Then
        Assert.Contains(vm.AvailableOpenClients, c => c.Client == GameClient.RedM && c.DisplayName == "RedM");
    }

    [Theory]
    [InlineData(GameClient.FiveMEnhanced, "Open FiveM Enhanced")]
    [InlineData(GameClient.RedM, "Open RedM")]
    public async Task Initialize_WhenStoredPreferredClientInstalled_ShouldSeedTheButtonFromIt(
        GameClient preferred, string label)
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { PreferredClient = preferred });

        // When
        var vm = await OpenClientViewModel(storage);

        // Then
        Assert.Equal(preferred, vm.PreferredClientOption?.Client);
        Assert.Equal(label, vm.OpenClientLabel);
    }

    [Fact]
    public async Task Initialize_WhenStoredPreferredClientNotInstalled_ShouldFallBackToFirstInstalled()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { PreferredClient = GameClient.FiveMEnhanced });

        // When
        var vm = await OpenClientViewModel(storage, new FakeClientInstallLocator());

        // Then
        Assert.Equal(GameClient.FiveM, vm.PreferredClientOption?.Client);
        Assert.Equal("Open FiveM", vm.OpenClientLabel);
    }

    [Fact]
    public async Task Initialize_WhenNoClientInstalled_ShouldSayNoClientInstalledAndDisableTheButton()
    {
        // Given
        var locator = new FakeClientInstallLocator { Executables = [] };

        // When
        var vm = await OpenClientViewModel(new InMemorySettingsStorage(), locator);

        // Then
        Assert.Empty(vm.AvailableOpenClients);
        Assert.Null(vm.PreferredClientOption);
        Assert.Equal("No client installed", vm.OpenClientLabel);
        Assert.False(vm.HasInstalledClients);
        Assert.False(vm.OpenClientCommand.CanExecute(null));
    }

    [Fact]
    public async Task ChoosePreferredClientCommand_ShouldUpdateBothEditorsAndPersistOnce()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = await OpenClientViewModel(storage);
        var savesBefore = storage.SaveCount;
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        var redm = vm.AvailableOpenClients.Single(c => c.Client == GameClient.RedM);

        // When — the button's dropdown picks a client.
        vm.ChoosePreferredClientCommand.Execute(redm);

        // Then — Settings (PreferredClientOption) and the button label both moved.
        Assert.Same(redm, vm.PreferredClientOption);
        Assert.Equal("Open RedM", vm.OpenClientLabel);
        Assert.Contains(nameof(MainViewModel.PreferredClientOption), raised);
        Assert.Contains(nameof(MainViewModel.OpenClientLabel), raised);
        Assert.Equal(savesBefore + 1, storage.SaveCount);
        Assert.Equal(GameClient.RedM, new ConfigurationRepository(storage).Load().PreferredClient);
    }

    [Fact]
    public async Task ChoosePreferredClientCommand_ShouldLaunchNothing()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = await OpenClientViewModel(new InMemorySettingsStorage(), processLauncher: processLauncher);

        // When
        vm.ChoosePreferredClientCommand.Execute(vm.AvailableOpenClients[1]);

        // Then
        Assert.Empty(processLauncher.ExecutableStarts);
        Assert.Empty(processLauncher.Requests);
    }

    [Fact]
    public async Task SettingPreferredClientOption_ShouldRelabelTheButtonAndPersistOnce()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = await OpenClientViewModel(storage);
        var savesBefore = storage.SaveCount;
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When — the Settings control changes the preferred client.
        vm.PreferredClientOption = vm.AvailableOpenClients.Single(c => c.Client == GameClient.FiveMEnhanced);

        // Then
        Assert.Equal("Open FiveM Enhanced", vm.OpenClientLabel);
        Assert.Contains(nameof(MainViewModel.OpenClientLabel), raised);
        Assert.Equal(savesBefore + 1, storage.SaveCount);
    }

    [Fact]
    public async Task ChoosePreferredClientCommand_WithTheCurrentClient_ShouldWriteNothing()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = await OpenClientViewModel(storage);
        var savesBefore = storage.SaveCount;

        // When
        vm.ChoosePreferredClientCommand.Execute(vm.PreferredClientOption);

        // Then
        Assert.Equal(savesBefore, storage.SaveCount);
    }

    [Fact]
    public async Task ChoosePreferredClientCommand_WithNonOptionParameter_ShouldKeepThePreferredClient()
    {
        // Given
        var storage = new InMemorySettingsStorage();
        var vm = await OpenClientViewModel(storage);
        var savesBefore = storage.SaveCount;

        // When
        vm.ChoosePreferredClientCommand.Execute("FiveM Enhanced");

        // Then
        Assert.Equal(GameClient.FiveM, vm.PreferredClientOption?.Client);
        Assert.Equal(savesBefore, storage.SaveCount);
    }

    [Fact]
    public async Task ChoosePreferredClientCommand_WhenSaveFails_ShouldRevertBothEditorsAndShowStatus()
    {
        // Given
        var storage = new ThrowingSettingsStorage();
        var vm = await OpenClientViewModel(storage);
        var before = vm.PreferredClientOption;
        storage.SaveFailure = new UnauthorizedAccessException("read-only");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.ChoosePreferredClientCommand.Execute(vm.AvailableOpenClients[1]);

        // Then — both bindings snap back to what the store still holds.
        Assert.Same(before, vm.PreferredClientOption);
        Assert.Equal("Open FiveM", vm.OpenClientLabel);
        Assert.Contains(nameof(MainViewModel.PreferredClientOption), raised);
        Assert.Contains(nameof(MainViewModel.OpenClientLabel), raised);
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Fact]
    public async Task OpenClientCommand_ShouldOpenThePreferredClientChosenInTheDropdown()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var vm = await OpenClientViewModel(new InMemorySettingsStorage(), processLauncher: processLauncher);
        vm.ChoosePreferredClientCommand.Execute(vm.AvailableOpenClients.Single(c => c.Client == GameClient.RedM));

        // When
        await vm.OpenClientAsync();

        // Then
        Assert.Equal(RedMExecutablePath, Assert.Single(processLauncher.ExecutableStarts));
        Assert.Equal("Opening RedM...", vm.StatusText);
    }

    [Fact]
    public async Task OpenClientCommand_WhenPreferredClientUninstalledSinceStartup_ShouldShowNotInstalledStatus()
    {
        // Given
        var processLauncher = new FakeGameProcessLauncher();
        var locator = new FakeClientInstallLocator();
        var vm = await OpenClientViewModel(new InMemorySettingsStorage(), locator, processLauncher);
        locator.Executables.Clear();

        // When
        await vm.OpenClientAsync();

        // Then
        Assert.Empty(processLauncher.ExecutableStarts);
        Assert.Equal("FiveM is not installed", vm.StatusText);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task OpenClientLabel_ShouldFollowTheLanguage()
    {
        // Given
        var vm = await OpenClientViewModel(new InMemorySettingsStorage());
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.SelectedLanguageOption = vm.LanguageOptions.Single(o => o.Tag == "es");

        // Then
        Assert.Equal("Abrir FiveM", vm.OpenClientLabel);
        Assert.Contains(nameof(MainViewModel.OpenClientLabel), raised);
    }
}
