using FiveMServerLauncher.Service;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Localization;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

// Settings page (ui-redesign ticket 09): the session-only General / Dev mode tab, the
// on-demand Check for updates and the two-way Dev mode client.
public partial class MainViewModelTests
{
    [Fact]
    public void SettingsTab_ShouldStartOnGeneral()
    {
        // Given / When
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // Then
        Assert.Equal(SettingsTab.General, vm.SettingsTab);
        Assert.True(vm.IsGeneralSettingsTab);
        Assert.False(vm.IsDevModeSettingsTab);
    }

    [Fact]
    public void SettingsTab_WhenSwitchedToDevMode_ShouldRaiseTheTabAndBothFlags()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.SettingsTab = SettingsTab.DevMode;

        // Then
        Assert.True(vm.IsDevModeSettingsTab);
        Assert.False(vm.IsGeneralSettingsTab);
        Assert.Contains(nameof(MainViewModel.SettingsTab), raised);
        Assert.Contains(nameof(MainViewModel.IsGeneralSettingsTab), raised);
        Assert.Contains(nameof(MainViewModel.IsDevModeSettingsTab), raised);
    }

    [Fact]
    public async Task SettingsTab_ShouldSurviveAPageSwitch()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.NavigateAsync(AppPage.Settings);
        vm.SettingsTab = SettingsTab.DevMode;

        // When
        await vm.NavigateAsync(AppPage.Servers);
        await vm.NavigateAsync(AppPage.Settings);

        // Then
        Assert.Equal(SettingsTab.DevMode, vm.SettingsTab);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenFeedAnswersNull_ShouldSayUpToDateAndShowNoPill()
    {
        // Given: the startup check already ran and found nothing
        var feed = new FakeReleaseFeed { Update = null };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"),
            releaseFeed: feed, updateApplier: new FakeUpdateApplier());
        await vm.InitializeAsync();
        Assert.Equal(string.Empty, vm.UpdateCheckText);

        // When
        await vm.CheckForUpdatesAsync();

        // Then
        Assert.Equal("You're up to date", vm.UpdateCheckText);
        Assert.False(vm.IsUpdateAvailable);
        Assert.Equal(2, feed.CheckCalls);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenUpdateFound_ShouldShowThePillLikeTheStartupCheck()
    {
        // Given: nothing at startup, a release published since
        var feed = new FakeReleaseFeed { Update = null };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"),
            releaseFeed: feed, updateApplier: new FakeUpdateApplier());
        await vm.InitializeAsync();
        feed.Update = NewerUpdate();

        // When
        await vm.CheckForUpdatesAsync();

        // Then: the title-bar pill state, and no row line (the pill carries the news)
        Assert.True(vm.IsUpdateAvailable);
        Assert.False(vm.IsUpdateFailed);
        Assert.Contains("v1.4.0", vm.UpdateBannerText);
        Assert.Equal(string.Empty, vm.UpdateCheckText);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenFeedThrows_ShouldSayCouldNotCheck()
    {
        // Given
        var feed = new FakeReleaseFeed { Update = null };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"),
            releaseFeed: feed, updateApplier: new FakeUpdateApplier());
        await vm.InitializeAsync();
        feed.ThrowOnCheckCount = 1;

        // When
        await vm.CheckForUpdatesAsync();

        // Then
        Assert.Equal("Could not check for updates", vm.UpdateCheckText);
        Assert.False(vm.IsUpdateAvailable);
        Assert.False(vm.IsCheckingForUpdates);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_AfterAFailedUpdate_ShouldReplaceTheFailedPillWithTheFoundOne()
    {
        // Given: an update failed to apply
        var feed = new FakeReleaseFeed { Update = NewerUpdate() };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"),
            releaseFeed: feed, updateApplier: new FakeUpdateApplier { ApplyResult = false });
        await vm.InitializeAsync();
        await vm.ApplyUpdateAsync();
        Assert.True(vm.IsUpdateFailed);

        // When
        await vm.CheckForUpdatesAsync();

        // Then: the pill states stay mutually exclusive
        Assert.True(vm.IsUpdateAvailable);
        Assert.False(vm.IsUpdateFailed);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_WhileAManualCheckRuns_ShouldBeGatedAndShowChecking()
    {
        // Given
        var feed = new FakeReleaseFeed { Update = null };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"),
            releaseFeed: feed, updateApplier: new FakeUpdateApplier());
        await vm.InitializeAsync();
        feed.PendingCheck = new TaskCompletionSource<LauncherUpdate?>();

        // When
        var check = vm.CheckForUpdatesAsync();

        // Then: busy and gated while in flight, a second call is ignored
        Assert.True(vm.IsCheckingForUpdates);
        Assert.False(vm.CheckForUpdatesCommand.CanExecute(null));
        Assert.Equal("Checking for updates...", vm.UpdateCheckText);
        await vm.CheckForUpdatesAsync();
        Assert.Equal(2, feed.CheckCalls);

        feed.PendingCheck.SetResult(null);
        await check;
        Assert.False(vm.IsCheckingForUpdates);
        Assert.True(vm.CheckForUpdatesCommand.CanExecute(null));
    }

    [Fact]
    public async Task CheckForUpdatesCommand_WhileTheStartupCheckRuns_ShouldBeGated()
    {
        // Given: the startup check is still waiting on GitHub
        var feed = new FakeReleaseFeed { PendingCheck = new TaskCompletionSource<LauncherUpdate?>() };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"),
            releaseFeed: feed, updateApplier: new FakeUpdateApplier());

        // When
        var startup = vm.InitializeAsync();

        // Then: gated, silent (the startup check never writes the row line)
        Assert.False(vm.CheckForUpdatesCommand.CanExecute(null));
        Assert.Equal(string.Empty, vm.UpdateCheckText);

        feed.PendingCheck.SetResult(null);
        await startup;
        Assert.True(vm.CheckForUpdatesCommand.CanExecute(null));
        Assert.Equal(string.Empty, vm.UpdateCheckText);
    }

    [Fact]
    public async Task CheckForUpdatesCommand_WhileAnUpdateIsApplying_ShouldBeGated()
    {
        // Given
        var applier = new FakeUpdateApplier { PendingApply = new TaskCompletionSource<bool>() };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"),
            releaseFeed: new FakeReleaseFeed { Update = NewerUpdate() }, updateApplier: applier);
        await vm.InitializeAsync();

        // When
        var apply = vm.ApplyUpdateAsync();

        // Then
        Assert.False(vm.CheckForUpdatesCommand.CanExecute(null));

        applier.PendingApply.SetResult(true);
        await apply;
    }

    [Fact]
    public async Task CheckForUpdatesCommand_WhenUpdateFeatureNotWired_ShouldBeDisabledAndDoNothing()
    {
        // Given: no feed/applier (the button shows disabled)
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When
        await vm.CheckForUpdatesAsync();

        // Then
        Assert.False(vm.CheckForUpdatesCommand.CanExecute(null));
        Assert.Equal(string.Empty, vm.UpdateCheckText);
        Assert.False(vm.IsCheckingForUpdates);
    }

    [Fact]
    public async Task UpdateCheckText_WhenLanguageSwitched_ShouldRefresh()
    {
        // Given
        var localizer = TestLocalizer.For(
            "en-US",
            ("en", """{ "UpdateCheckUpToDate": "You're up to date" }"""),
            ("es", """{ "UpdateCheckUpToDate": "Estás al día" }"""));
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), localizer: localizer,
            releaseFeed: new FakeReleaseFeed(), updateApplier: new FakeUpdateApplier());
        await vm.CheckForUpdatesAsync();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        localizer.SetLanguage("es");

        // Then
        Assert.Equal("Estás al día", vm.UpdateCheckText);
        Assert.Contains(nameof(MainViewModel.UpdateCheckText), raised);
    }

    [Fact]
    public void DevClient_WhenSetToRedM_ShouldSwitchTheDevTargetAndItsBuildOptions()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.DevClient = GameClient.RedM;

        // Then
        Assert.Equal(GameClient.RedM, vm.DevClient);
        Assert.Contains(nameof(MainViewModel.DevClient), raised);
        Assert.Contains(nameof(MainViewModel.DevGameBuildOptions), raised);
    }

    [Fact]
    public void DevClient_WhenSetToEnhanced_ShouldBeRefused()
    {
        // Given: Dev mode opens only Legacy FiveM or RedM
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // When
        vm.DevClient = GameClient.FiveMEnhanced;

        // Then
        Assert.Equal(GameClient.FiveM, vm.DevClient);
    }
}
