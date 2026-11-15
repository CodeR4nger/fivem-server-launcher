using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.ViewModels;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// The opt-in cfxlauncher:// registration: the registry change and the persisted setting move
// together, and a failure on either side leaves both as they were with one status line.
public partial class MainViewModelTests
{
    private const string WebLinksExe = @"D:\Games\CFX\CFXLauncher.exe";
    private const string WebLinksCommand = @"""D:\Games\CFX\CFXLauncher.exe"" ""%1""";
    private const string WebLinksFailed = "Could not register web links";

    private static MainViewModel WebLinksViewModel(
        FakeUriSchemeRegistration registry,
        ISettingsStorage storage,
        string? exe = WebLinksExe)
    {
        return CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            settings: new ConfigurationRepository(storage),
            webLinks: new LauncherSchemeRegistrar(registry, () => exe));
    }

    [Fact]
    public void WebLinks_WhenEnabled_ShouldRegisterTheSchemeAndPersist()
    {
        // Given
        var registry = new FakeUriSchemeRegistration();
        var storage = new InMemorySettingsStorage();
        var vm = WebLinksViewModel(registry, storage);

        // When
        vm.WebLinks = true;

        // Then
        Assert.Equal(WebLinksCommand, registry.UserCommands[LauncherUri.Scheme]);
        Assert.True(new ConfigurationRepository(storage).Load().WebLinks);
    }

    [Fact]
    public void WebLinks_WhenDisabled_ShouldRemoveOurRegistrationAndPersist()
    {
        // Given
        var registry = new FakeUriSchemeRegistration();
        registry.UserCommands[LauncherUri.Scheme] = WebLinksCommand;
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { WebLinks = true });
        var vm = WebLinksViewModel(registry, storage);

        // When
        vm.WebLinks = false;

        // Then
        Assert.False(registry.UserCommands.ContainsKey(LauncherUri.Scheme));
        Assert.False(new ConfigurationRepository(storage).Load().WebLinks);
    }

    [Fact]
    public void WebLinks_WhenRegistryWriteFails_ShouldRevertAndShowStatus()
    {
        // Given
        var registry = new FakeUriSchemeRegistration { Failure = new UnauthorizedAccessException("denied") };
        var storage = new InMemorySettingsStorage();
        var vm = WebLinksViewModel(registry, storage);
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.WebLinks = true;

        // Then — the checkbox snaps back and nothing was persisted.
        Assert.False(vm.WebLinks);
        Assert.Contains(nameof(MainViewModel.WebLinks), raised);
        Assert.Equal(WebLinksFailed, vm.StatusText);
        Assert.False(new ConfigurationRepository(storage).Load().WebLinks);
    }

    [Fact]
    public void WebLinks_WhenAnotherProgramOwnsTheScheme_ShouldRevertAndShowStatus()
    {
        // Given
        const string foreign = @"""C:\Other\OtherApp.exe"" ""%1""";
        var registry = new FakeUriSchemeRegistration();
        registry.UserCommands[LauncherUri.Scheme] = foreign;
        var vm = WebLinksViewModel(registry, new InMemorySettingsStorage());

        // When
        vm.WebLinks = true;

        // Then
        Assert.False(vm.WebLinks);
        Assert.Equal(WebLinksFailed, vm.StatusText);
        Assert.Equal(foreign, registry.UserCommands[LauncherUri.Scheme]);
    }

    [Fact]
    public void WebLinks_WhenDisableFailsInRegistry_ShouldStayEnabledAndShowStatus()
    {
        // Given
        var registry = new FakeUriSchemeRegistration();
        registry.UserCommands[LauncherUri.Scheme] = WebLinksCommand;
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { WebLinks = true });
        var vm = WebLinksViewModel(registry, storage);
        registry.Failure = new IOException("locked");

        // When
        vm.WebLinks = false;

        // Then
        Assert.True(vm.WebLinks);
        Assert.Equal(WebLinksFailed, vm.StatusText);
        Assert.True(new ConfigurationRepository(storage).Load().WebLinks);
    }

    [Fact]
    public void WebLinks_WhenSettingsSaveFails_ShouldUndoTheRegistration()
    {
        // Given
        var registry = new FakeUriSchemeRegistration();
        var storage = new ThrowingSettingsStorage { SaveFailure = new IOException("locked") };
        var vm = WebLinksViewModel(registry, storage);

        // When
        vm.WebLinks = true;

        // Then — the registry does not keep what the settings could not record.
        Assert.False(vm.WebLinks);
        Assert.False(registry.UserCommands.ContainsKey(LauncherUri.Scheme));
        Assert.Equal(SaveFailed, vm.StatusText);
    }

    [Fact]
    public async Task Initialize_WithWebLinksOnAndMovedExe_ShouldRewriteTheRegistration()
    {
        // Given: registered by this launcher from its previous folder
        var registry = new FakeUriSchemeRegistration();
        registry.UserCommands[LauncherUri.Scheme] = @"""C:\Old\CFXLauncher.exe"" ""%1""";
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { WebLinks = true });
        var vm = WebLinksViewModel(registry, storage);

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(WebLinksCommand, registry.UserCommands[LauncherUri.Scheme]);
    }

    [Fact]
    public async Task Initialize_WithWebLinksOff_ShouldNotTouchTheRegistry()
    {
        // Given
        var registry = new FakeUriSchemeRegistration();
        registry.UserCommands[LauncherUri.Scheme] = @"""C:\Old\CFXLauncher.exe"" ""%1""";
        var vm = WebLinksViewModel(registry, new InMemorySettingsStorage());

        // When
        await vm.InitializeAsync();

        // Then
        Assert.Equal(0, registry.Writes);
        Assert.Equal(0, registry.Removals);
    }

    [Fact]
    public async Task Initialize_WhenStartupRefreshFails_ShouldStartQuietlyWithTheSettingOn()
    {
        // Given
        var registry = new FakeUriSchemeRegistration { Failure = new UnauthorizedAccessException("denied") };
        var storage = new InMemorySettingsStorage();
        storage.Save(new LauncherSettings { WebLinks = true });
        var vm = WebLinksViewModel(registry, storage);

        // When
        await vm.InitializeAsync();

        // Then
        Assert.True(vm.WebLinks);
        Assert.Equal("Ready", vm.StatusText);
    }
}
