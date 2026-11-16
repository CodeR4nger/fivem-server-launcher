using System.Windows;
using System.Windows.Controls;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Views;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// The bottom bar's open-client split button in the real window at 1000×640: its widest
// labels fit in every shipped language without touching the nav tabs, and its client menu
// lists the installed clients with the preferred one marked.
public partial class MainViewModelTests
{
    public enum OpenClientLabelCase
    {
        LongestClientName,
        NoClientInstalled
    }

    public static TheoryData<string, OpenClientLabelCase> ShippedLanguagesTimesOpenClientLabels()
    {
        var data = new TheoryData<string, OpenClientLabelCase>();

        foreach (var language in Localizer.ShippedLanguageOptions)
        {
            foreach (var labelCase in Enum.GetValues<OpenClientLabelCase>())
            {
                data.Add(language.Tag, labelCase);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ShippedLanguagesTimesOpenClientLabels))]
    public void OpenClientButton_AtMinimumSize_ShouldShowItsWidestLabelWithoutClippingOrOverlappingTheTabs(
        string languageTag, OpenClientLabelCase labelCase)
    {
        // Given the real window at its minimum size, with FiveM Enhanced preferred (the
        // longest client name) or no client installed at all
        var locator = labelCase == OpenClientLabelCase.NoClientInstalled
            ? new FakeClientInstallLocator { Executables = [] }
            : AllClientsInstalled();

        var (label, problems) = RunOnSta(() =>
        {
            var (window, vm) = ShowShell(languageTag, installLocator: locator);

            try
            {
                if (labelCase == OpenClientLabelCase.LongestClientName)
                {
                    vm.PreferredClientOption = vm.AvailableOpenClients.Single(c => c.Client == GameClient.FiveMEnhanced);
                }

                window.UpdateLayout();

                // When the bottom bar is measured
                var shell = (MainView)window.FindName("ShellContent");
                var bar = (BottomBar)shell.FindName("BottomBar");
                var button = FindDescendant<OpenClientButton>(bar);
                var tabs = (FrameworkElement)bar.FindName("NavTrack");
                var problems = RegionProblems(window, "bottom bar", bar).ToList();
                var buttonBounds = button.TransformToAncestor(bar).TransformBounds(new Rect(button.RenderSize));
                var tabsBounds = tabs.TransformToAncestor(bar).TransformBounds(new Rect(tabs.RenderSize));

                if (buttonBounds.Left < tabsBounds.Right - 0.5)
                {
                    problems.Add($"the open-client button {buttonBounds:0} overlaps the nav tabs {tabsBounds:0}");
                }

                var openButton = (Button)button.FindName("OpenButton");
                return (openButton.Content as string, problems);
            }
            finally
            {
                window.Close();
            }
        });

        // Then the label is the expected one and nothing in the bar is cut or overlapping
        var localizer = Localizer.FromEmbeddedResources();
        localizer.SetLanguage(languageTag);
        var expected = labelCase == OpenClientLabelCase.NoClientInstalled
            ? localizer.Get("OpenClientNone")
            : localizer.Format("OpenClientButton", "FiveM Enhanced");
        Assert.Equal(expected, label);
        Assert.Empty(problems);
    }

    // The menu tests lay the real ClientMenu out WITHOUT opening it: an open ContextMenu is a
    // real popup window on the live desktop (menu mode, mouse capture, UI Automation events to
    // any client on the machine), which a unit test must not drive. Opening it is one WPF
    // property set in OpenMenu; what is tested is what the menu shows and what a pick does.
    [Fact]
    public void OpenClientMenu_ShouldListTheInstalledClientsAndMarkOnlyThePreferredOne()
    {
        // Given the shell with every client installed and RedM preferred
        var (items, marked) = RunOnSta(() =>
        {
            var (window, vm) = ShowShell("en", installLocator: AllClientsInstalled());

            try
            {
                vm.PreferredClientOption = vm.AvailableOpenClients.Single(c => c.Client == GameClient.RedM);

                // When the menu is laid out
                var menu = LaidOutClientMenu(FindDescendant<OpenClientButton>(window));
                var names = vm.AvailableOpenClients.Select(c => c.DisplayName).ToList();
                var markedNames = vm.AvailableOpenClients
                    .Select(c => (MenuItem)menu.ItemContainerGenerator.ContainerFromItem(c))
                    .Where(item => FindDescendants<TextBlock>(item).Any(t => t.Text == "✓" && t.Visibility == Visibility.Visible))
                    .Select(item => ((InstalledClientOption)item.DataContext).DisplayName)
                    .ToList();
                return (names, markedNames);
            }
            finally
            {
                window.Close();
            }
        });

        // Then it lists them in order and marks the preferred one only
        Assert.Equal(["FiveM", "FiveM Enhanced", "RedM"], items);
        Assert.Equal(["RedM"], marked);
    }

    [Fact]
    public void OpenClientMenu_PickingAClient_ShouldSaveItAsPreferredAndRelabelTheButton()
    {
        // Given the laid-out menu
        var (preferred, label) = RunOnSta(() =>
        {
            var (window, vm) = ShowShell("en", installLocator: AllClientsInstalled());

            try
            {
                var button = FindDescendant<OpenClientButton>(window);
                var menu = LaidOutClientMenu(button);
                var enhanced = vm.AvailableOpenClients.Single(c => c.Client == GameClient.FiveMEnhanced);
                var item = (MenuItem)menu.ItemContainerGenerator.ContainerFromItem(enhanced);

                // When a client is picked (what MenuItem.OnClick runs: its bound command)
                item.Command.Execute(item.CommandParameter);
                window.UpdateLayout();

                return (vm.PreferredClientOption?.Client, ((Button)button.FindName("OpenButton")).Content as string);
            }
            finally
            {
                window.Close();
            }
        });

        // Then it is the preferred client and the button says so
        Assert.Equal(GameClient.FiveMEnhanced, preferred);
        Assert.Equal("Open FiveM Enhanced", label);
    }

    private static ContextMenu LaidOutClientMenu(OpenClientButton button)
    {
        var menu = (ContextMenu)((FrameworkElement)button.FindName("ButtonRow")).Resources["ClientMenu"];
        menu.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        menu.Arrange(new Rect(menu.DesiredSize));
        menu.UpdateLayout();
        return menu;
    }

    [Fact]
    public void OpenClientMenu_WithNoClientInstalled_ShouldStayClosedAndDisableBothParts()
    {
        // Given the shell with no client installed
        var (openEnabled, chooserEnabled, open) = RunOnSta(() =>
        {
            var (window, _) = ShowShell("en", installLocator: new FakeClientInstallLocator { Executables = [] });

            try
            {
                var button = FindDescendant<OpenClientButton>(window);

                // When the menu is asked to open
                button.OpenMenu();
                StaThread.Pump(150);

                return (((Button)button.FindName("OpenButton")).IsEnabled,
                    ((Button)button.FindName("ChooserButton")).IsEnabled,
                    button.IsMenuOpen);
            }
            finally
            {
                window.Close();
            }
        });

        // Then nothing opens and neither part can be used
        Assert.False(openEnabled);
        Assert.False(chooserEnabled);
        Assert.False(open);
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        => FindDescendants<T>(root).First();

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);

            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindDescendants<T>(child))
            {
                yield return nested;
            }
        }
    }
}
