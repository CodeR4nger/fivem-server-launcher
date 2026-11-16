using System.Windows;
using System.Windows.Controls;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

// The Settings page (ticket 09) at the minimum window size (1000×640), in every shipped
// language, on both tabs: nothing is clipped, long descriptions wrap, and the segmented
// controls write the same state the rest of the shell reads.
public partial class MainViewModelTests
{
    private static readonly string[] GeneralTabControls =
    [
        "PreferredClientSegments", "AutoLaunchSwitch", "WebLinksSwitch", "MinimizeToTraySwitch",
        "LanguagePicker", "AnimationsSegments", "VersionText", "CheckForUpdatesButton", "UpdateCheckLine"
    ];

    private static readonly string[] DevModeTabControls =
    [
        "DevClientSegments", "DevGameBuildPicker", "DevPureModePicker", "DevLaunchButton", "DevLaunchSecondButton"
    ];

    public static TheoryData<string, SettingsTab> ShippedLanguagesTimesSettingsTabs()
    {
        var data = new TheoryData<string, SettingsTab>();

        foreach (var language in Localizer.ShippedLanguageOptions)
        {
            foreach (var tab in Enum.GetValues<SettingsTab>())
            {
                data.Add(language.Tag, tab);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ShippedLanguagesTimesSettingsTabs))]
    public void SettingsPage_AtMinimumSize_ShouldClipNothing(string languageTag, SettingsTab tab)
    {
        // Given the real window at its minimum size, in the given language, every client
        // installed (the widest segmented control) and the longest About line showing
        var problems = RunOnSta(() =>
        {
            var (window, vm) = ShowSettings(languageTag, new FakeReleaseFeed { ThrowOnCheckCount = 2 });

            try
            {
                vm.CheckForUpdatesAsync().GetAwaiter().GetResult();

                // When the tab is shown
                vm.SettingsTab = tab;
                window.UpdateLayout();

                var page = SettingsPage(window);
                var shown = (FrameworkElement)page.FindName(tab == SettingsTab.General ? "GeneralTab" : "DevModeTab");
                var hidden = (FrameworkElement)page.FindName(tab == SettingsTab.General ? "DevModeTab" : "GeneralTab");
                var scroller = (ScrollViewer)page.FindName("SettingsScroller");
                var problems = RegionProblems(window, "settings header", (FrameworkElement)page.FindName("SettingsHeader"))
                    .Concat(RegionProblems(window, $"{tab} tab", shown)
                        // The tab scrolls: it may run past the window, never past its viewport's width.
                        .Where(problem => !problem.Contains("reaches outside the window")))
                    .ToList();

                if (hidden.IsVisible)
                {
                    problems.Add("the other tab is shown too");
                }

                if (shown.ActualWidth > scroller.ViewportWidth + 0.5)
                {
                    problems.Add($"the tab is {shown.ActualWidth:0} wide in a {scroller.ViewportWidth:0} viewport");
                }

                foreach (var name in tab == SettingsTab.General ? GeneralTabControls : DevModeTabControls)
                {
                    if (page.FindName(name) is not FrameworkElement { IsVisible: true })
                    {
                        problems.Add($"{name} is not shown");
                    }
                }

                return problems;
            }
            finally
            {
                window.Close();
            }
        });

        // Then the header and the tab are whole (descriptions wrap instead of clipping)
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [Fact]
    public void SettingsPage_WithNoClientInstalled_ShouldShowTheNoteInsteadOfTheSegments()
    {
        // Given no client installed
        var (segments, note) = RunOnSta(() =>
        {
            var (window, _) = ShowSettings("en", installLocator: new FakeClientInstallLocator { Executables = [] });

            try
            {
                var page = SettingsPage(window);
                return (((FrameworkElement)page.FindName("PreferredClientSegments")).IsVisible,
                    ((TextBlock)page.FindName("NoClientNote")).Text);
            }
            finally
            {
                window.Close();
            }
        });

        // Then the row says so instead of an empty control
        Assert.False(segments);
        Assert.Equal("No client installed", note);
    }

    [Fact]
    public void PreferredClientSegments_ShouldShareTheStateOfTheOpenClientButton()
    {
        // Given every client installed, FiveM preferred
        var (fromSegments, fromViewModel) = RunOnSta(() =>
        {
            var (window, vm) = ShowSettings("en");

            try
            {
                var segments = (ListBox)SettingsPage(window).FindName("PreferredClientSegments");

                // When RedM is picked in Settings
                segments.SelectedItem = vm.AvailableOpenClients.Single(c => c.Client == GameClient.RedM);
                var label = vm.OpenClientLabel;

                // and Enhanced is picked from the bottom-bar button
                vm.ChoosePreferredClientCommand.Execute(
                    vm.AvailableOpenClients.Single(c => c.Client == GameClient.FiveMEnhanced));
                window.UpdateLayout();

                return (label, ((InstalledClientOption)segments.SelectedItem).Client);
            }
            finally
            {
                window.Close();
            }
        });

        // Then each side shows the other's pick
        Assert.Equal("Open RedM", fromSegments);
        Assert.Equal(GameClient.FiveMEnhanced, fromViewModel);
    }

    [Fact]
    public void AnimationsSegments_WhenMinimalPicked_ShouldTurnMotionOffAtOnce()
    {
        // Given motion on
        var (setting, motion) = RunOnSta(() =>
        {
            var (window, vm) = ShowSettings("en");

            try
            {
                Assert.True(Motion.GetIsEnabled(window));
                var segments = (ListBox)SettingsPage(window).FindName("AnimationsSegments");

                // When Minimal is picked
                segments.SelectedValue = InterfaceAnimations.Minimal;

                return (vm.InterfaceAnimations, Motion.GetIsEnabled(window));
            }
            finally
            {
                window.Close();
            }
        });

        // Then the setting is Minimal and the window's motion switch is already off
        Assert.Equal(InterfaceAnimations.Minimal, setting);
        Assert.False(motion);
    }

    [Fact]
    public void SettingsTabs_WhenDevModePicked_ShouldShowTheDevModeTab()
    {
        // Given the Settings page on General
        var (current, devShown) = RunOnSta(() =>
        {
            var (window, vm) = ShowSettings("en");

            try
            {
                var page = SettingsPage(window);

                // When the Dev mode tab is picked
                ((ListBox)page.FindName("SettingsTabs")).SelectedValue = SettingsTab.DevMode;
                window.UpdateLayout();

                return (vm.SettingsTab, ((FrameworkElement)page.FindName("DevModeTab")).IsVisible);
            }
            finally
            {
                window.Close();
            }
        });

        // Then
        Assert.Equal(SettingsTab.DevMode, current);
        Assert.True(devShown);
    }

    [Fact]
    public void DevClientSegments_WhenRedMPicked_ShouldSwitchTheDevClient()
    {
        // Given the Dev mode tab
        var client = RunOnSta(() =>
        {
            var (window, vm) = ShowSettings("en");

            try
            {
                vm.SettingsTab = SettingsTab.DevMode;
                window.UpdateLayout();

                // When RedM is picked
                ((ListBox)SettingsPage(window).FindName("DevClientSegments")).SelectedValue = GameClient.RedM;

                return vm.DevClient;
            }
            finally
            {
                window.Close();
            }
        });

        // Then
        Assert.Equal(GameClient.RedM, client);
    }

    private static SettingsPage SettingsPage(MainWindow window) =>
        (SettingsPage)((MainView)window.FindName("ShellContent")).FindName("SettingsPage");

    private static (MainWindow Window, MainViewModel ViewModel) ShowSettings(
        string languageTag,
        FakeReleaseFeed? releaseFeed = null,
        FakeClientInstallLocator? installLocator = null)
    {
        var (window, vm) = ShowShell(
            languageTag, installLocator: installLocator ?? AllClientsInstalled(), releaseFeed: releaseFeed);
        vm.NavigateAsync(AppPage.Settings).GetAwaiter().GetResult();
        window.UpdateLayout();
        return (window, vm);
    }
}
