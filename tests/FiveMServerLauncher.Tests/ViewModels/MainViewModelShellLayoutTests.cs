using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.Tests.Views;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

// The shell at its minimum size (1000×640), in every shipped language, on every page,
// with the update pill showing its widest state: nothing in the title bar, the bottom bar
// or the footer is cut.
public partial class MainViewModelTests
{
    public static TheoryData<string, AppPage> ShippedLanguagesTimesPages()
    {
        var data = new TheoryData<string, AppPage>();

        foreach (var language in Localizer.ShippedLanguageOptions)
        {
            foreach (var page in Enum.GetValues<AppPage>())
            {
                data.Add(language.Tag, page);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ShippedLanguagesTimesPages))]
    public void Shell_AtMinimumSize_ShouldShowThePageAndClipNothingInItsBars(string languageTag, AppPage page)
    {
        // Given the real window at its minimum size, in the given language, with an update
        // available (the widest pill: text, UPDATE and dismiss)
        var problems = RunOnSta(() =>
        {
            var (window, vm) = ShowShell(languageTag);

            try
            {
                // When the page is shown
                vm.NavigateAsync(page).GetAwaiter().GetResult();
                window.UpdateLayout();

                var problems = new List<string>();

                if (window.ActualWidth > 1000.5 || window.ActualHeight > 640.5)
                {
                    problems.Add($"window opened at {window.ActualWidth}×{window.ActualHeight}, not 1000×640");
                }

                var shell = (MainView)window.FindName("ShellContent");
                problems.AddRange(PageProblems(shell, page));

                foreach (var (name, region) in new (string, FrameworkElement)[]
                         {
                             ("title bar", (FrameworkElement)window.FindName("TitleBar")),
                             ("bottom bar", (FrameworkElement)shell.FindName("BottomBar")),
                             ("footer", (FrameworkElement)window.FindName("Footer"))
                         })
                {
                    problems.AddRange(RegionProblems(window, name, region));
                }

                var pill = (FrameworkElement)window.FindName("UpdatePill");

                if (!pill.IsVisible)
                {
                    problems.Add("the update pill is not shown");
                }

                return problems;
            }
            finally
            {
                window.Close();
            }
        });

        // Then the page is the only one shown and every bar element is whole
        Assert.Empty(problems);
    }

    [Theory]
    [InlineData(AppPage.Servers, 0)]
    [InlineData(AppPage.Browse, 128)]
    [InlineData(AppPage.Settings, 256)]
    public void NavHighlight_WithMotionOff_ShouldSnapUnderTheCurrentTab(AppPage page, double expectedOffset)
    {
        // Given the shell with motion off
        var (offset, animated) = RunOnSta(() =>
        {
            var (window, vm) = ShowShell("en");

            try
            {
                Motion.SetIsEnabled(window, false);
                vm.NavigateAsync(AppPage.Settings).GetAwaiter().GetResult();

                // When the page changes
                vm.NavigateAsync(page).GetAwaiter().GetResult();

                var highlight = NavHighlightOffset(window);
                return (highlight.X, highlight.HasAnimatedProperties);
            }
            finally
            {
                window.Close();
            }
        });

        // Then the highlight sits under that tab at once
        Assert.Equal(expectedOffset, offset);
        Assert.False(animated);
    }

    [Fact]
    public void NavHighlight_WithMotionOn_ShouldSlideToTheCurrentTab()
    {
        // Given the shell with motion on
        var (animated, settled) = RunOnSta(() =>
        {
            var (window, vm) = ShowShell("en");

            try
            {
                Motion.SetIsEnabled(window, true);

                // When the page changes
                vm.NavigateAsync(AppPage.Settings).GetAwaiter().GetResult();
                var highlight = NavHighlightOffset(window);
                var sliding = highlight.HasAnimatedProperties;
                StaThread.Pump(500);

                return (sliding, highlight.X);
            }
            finally
            {
                window.Close();
            }
        });

        // Then it slides and comes to rest under the Settings tab
        Assert.True(animated);
        Assert.Equal(256, settled);
    }

    private static TranslateTransform NavHighlightOffset(MainWindow window)
    {
        var shell = (MainView)window.FindName("ShellContent");
        var bar = (BottomBar)shell.FindName("BottomBar");
        return (TranslateTransform)((UIElement)bar.FindName("NavHighlight")).RenderTransform;
    }

    private static (MainWindow Window, MainViewModel ViewModel) ShowShell(
        string languageTag,
        Tests.Configuration.InMemoryServerRepository? repository = null,
        FakeClientInstallLocator? installLocator = null,
        FakeReleaseFeed? releaseFeed = null)
    {
        var localizer = Localizer.FromEmbeddedResources();
        LocalizationSource.Instance.Attach(localizer);
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            installLocator: installLocator,
            localizer: localizer,
            repository: repository,
            releaseFeed: releaseFeed ?? new FakeReleaseFeed { Update = NewerUpdate() },
            updateApplier: new FakeUpdateApplier());
        vm.InitializeAsync().GetAwaiter().GetResult();

        // After the view model: it applies the saved language (none = system) on
        // construction, which would otherwise override the language under test.
        localizer.SetLanguage(languageTag);
        var window = new MainWindow
        {
            DataContext = vm,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
            Top = -10000
        };
        window.Show();
        window.UpdateLayout();
        return (window, vm);
    }

    private static IEnumerable<string> PageProblems(MainView shell, AppPage page)
    {
        var pages = new Dictionary<AppPage, string>
        {
            [AppPage.Servers] = "ServersPage",
            [AppPage.Browse] = "BrowsePage",
            [AppPage.Settings] = "SettingsPage"
        };

        foreach (var (candidate, name) in pages)
        {
            var element = (FrameworkElement)shell.FindName(name);

            if (element.IsVisible != (candidate == page))
            {
                yield return $"{name} visible={element.IsVisible} on the {page} page";
            }
        }

        var shown = (FrameworkElement)shell.FindName(pages[page]);
        var panel = (FrameworkElement)shell.FindName("ContentPanel");
        var bounds = shown.TransformToAncestor(panel).TransformBounds(new Rect(shown.RenderSize));

        if (bounds.Width < panel.ActualWidth - 4 || bounds.Height < panel.ActualHeight - 4)
        {
            yield return $"{pages[page]} fills only {bounds.Width:0}×{bounds.Height:0} of the {panel.ActualWidth:0}×{panel.ActualHeight:0} panel";
        }
    }

    // A region is clipped when any visible element in it is arranged larger than its slot
    // (WPF clips it there), when a text is wider than its box, or when anything reaches
    // outside the region or the region outside the window.
    private static IEnumerable<string> RegionProblems(Window window, string name, FrameworkElement region)
    {
        var root = (FrameworkElement)window.Content;
        var inWindow = region.TransformToAncestor(root).TransformBounds(new Rect(region.RenderSize));

        if (inWindow.Left < -0.5 || inWindow.Top < -0.5
            || inWindow.Right > root.ActualWidth + 0.5 || inWindow.Bottom > root.ActualHeight + 0.5)
        {
            yield return $"the {name} reaches outside the window ({inWindow})";
        }

        foreach (var element in VisibleDescendants(region))
        {
            var label = Describe(element);
            // An element arranged larger than its layout slot is clipped to the slot.
            var slot = LayoutInformation.GetLayoutSlot(element);
            var room = new Size(
                Math.Max(0, slot.Width - element.Margin.Left - element.Margin.Right),
                Math.Max(0, slot.Height - element.Margin.Top - element.Margin.Bottom));

            if (element.RenderSize.Width > room.Width + 0.5 || element.RenderSize.Height > room.Height + 0.5)
            {
                yield return $"{name}: {label} is {element.RenderSize.Width:0}×{element.RenderSize.Height:0} in a {room.Width:0}×{room.Height:0} slot";
            }

            // WPF clamps DesiredSize to the space it was offered, so a cut text is found by
            // its natural (unconstrained) width instead. A wrapping text is not cut by being
            // narrower than its natural width (its height is checked against the slot above),
            // and an ellipsized text (a server name, an address) is shortened on purpose.
            if (element is TextBlock { TextWrapping: TextWrapping.NoWrap, TextTrimming: TextTrimming.None } text && NaturalWidth(text) > text.RenderSize.Width - text.Padding.Left - text.Padding.Right + 0.5)
            {
                yield return $"{name}: text '{text.Text}' is cut ({NaturalWidth(text):0} > {text.RenderSize.Width:0})";
            }

            var bounds = element.TransformToAncestor(region).TransformBounds(new Rect(element.RenderSize));

            if (bounds.Left < -0.5 || bounds.Right > region.ActualWidth + 0.5
                || bounds.Top < -0.5 || bounds.Bottom > region.ActualHeight + 0.5)
            {
                yield return $"{name}: {label} reaches outside it ({bounds:0})";
            }
        }
    }

    private static IEnumerable<FrameworkElement> VisibleDescendants(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (VisualTreeHelper.GetChild(root, i) is not FrameworkElement { IsVisible: true } child)
            {
                continue;
            }

            yield return child;

            foreach (var nested in VisibleDescendants(child))
            {
                yield return nested;
            }
        }
    }

    private static double NaturalWidth(TextBlock text) => new FormattedText(
            text.Text,
            System.Globalization.CultureInfo.CurrentUICulture,
            text.FlowDirection,
            new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch),
            text.FontSize,
            Brushes.Black,
            VisualTreeHelper.GetDpi(text).PixelsPerDip)
        .WidthIncludingTrailingWhitespace;

    private static string Describe(FrameworkElement element) => element switch
    {
        TextBlock text => $"text '{text.Text}'",
        ContentControl { Content: string content } => $"{element.GetType().Name} '{content}'",
        _ => string.IsNullOrEmpty(element.Name) ? element.GetType().Name : $"{element.GetType().Name} {element.Name}"
    };
}
