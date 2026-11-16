using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

// The Browse page at the window's minimum size (1000×640), in every shipped language: the
// header, the filter row and a saved row with a very long name show every control whole.
public partial class MainViewModelTests
{
    private const string LongServerName =
        "Los Santos Roleplay | Serious RP | Custom Cars | Jobs | Gangs | Economy | Whitelisted | discord.gg/lsrp";

    [Theory]
    [MemberData(nameof(ShippedLanguageTags))]
    public void BrowsePage_AtMinimumSize_ShouldClipNothingInTheHeaderFiltersOrALongRow(string languageTag)
    {
        // Given the real window at 1000×640 on the Browse page, a saved server with a very
        // long name first in the list
        var problems = RunOnSta(() =>
        {
            var (window, vm) = ShowBrowse(languageTag);

            try
            {
                var page = BrowsePage(window);
                var problems = new List<string>();

                // When the header, the filter row and the long row are measured
                foreach (var region in new[] { "BrowseHeader", "BrowseFilters" })
                {
                    problems.AddRange(RegionProblems(window, region, (FrameworkElement)page.FindName(region)));
                }

                var row = RealizedRow(page, vm.Browser.Servers.Single(s => s.Name == LongServerName));
                problems.AddRange(RegionProblems(window, "long row", row));

                // The long name is ellipsized, never pushing the tag, players or buttons out
                var name = Descendants<TextBlock>(row).First(t => t.Text == LongServerName);

                if (name.TextTrimming == TextTrimming.None || name.ActualWidth >= NaturalWidth(name))
                {
                    problems.Add("the long name is not ellipsized");
                }

                // ...and the buttons never squeeze the name to a sliver
                if (name.ActualWidth < 240)
                {
                    problems.Add($"the name keeps only {name.ActualWidth:0} px");
                }

                var list = (FrameworkElement)page.FindName("BrowserList");

                if (row.ActualWidth > list.ActualWidth + 0.5)
                {
                    problems.Add($"the row is {row.ActualWidth:0} wide in a {list.ActualWidth:0} list");
                }

                return problems;
            }
            finally
            {
                window.Close();
            }
        });

        // Then nothing is cut
        Assert.Empty(problems);
    }

    [Fact]
    public void BrowsePage_List_ShouldStayVirtualizedAndRecycling()
    {
        // Given the Browse page with rows
        var (virtualizing, recycling, contentScroll, realized, total) = RunOnSta(() =>
        {
            var (window, vm) = ShowBrowse("en", extraRows: 500);

            try
            {
                var list = (ListBox)BrowsePage(window).FindName("BrowserList");
                var viewer = Descendants<ScrollViewer>(list).First();
                var contentScrolls = true;

                // When every ancestor of the list's panel is checked for pixel scrolling
                for (DependencyObject? node = list; node is not null; node = VisualTreeHelper.GetParent(node))
                {
                    contentScrolls &= ScrollViewer.GetCanContentScroll(node) || node is not ScrollViewer;
                }

                return (VirtualizingPanel.GetIsVirtualizing(list),
                    VirtualizingPanel.GetVirtualizationMode(list) == VirtualizationMode.Recycling,
                    contentScrolls && viewer.CanContentScroll,
                    Descendants<ListBoxItem>(list).Count(),
                    vm.Browser.Servers.Count);
            }
            finally
            {
                window.Close();
            }
        });

        // Then only a page of rows is realized and the panel recycles them
        Assert.True(virtualizing);
        Assert.True(recycling);
        Assert.True(contentScroll);
        Assert.InRange(realized, 1, 40);
        Assert.True(total > 500);
    }

    [Fact]
    public void BrowsePage_ScrollPosition_ShouldSurviveAPageSwitch()
    {
        // Given the Browse page scrolled down a long list
        var (before, after, topBefore, topAfter) = RunOnSta(() =>
        {
            var (window, vm) = ShowBrowse("en", extraRows: 500);

            try
            {
                var list = (ListBox)BrowsePage(window).FindName("BrowserList");
                var viewer = Descendants<ScrollViewer>(list).First();
                viewer.ScrollToVerticalOffset(250);
                window.UpdateLayout();
                var scrolled = viewer.VerticalOffset;
                var top = list.Items[(int)viewer.VerticalOffset];

                // When the player visits Servers and comes back
                PumpUntilDone(vm.NavigateAsync(AppPage.Servers));
                window.UpdateLayout();
                PumpUntilDone(vm.NavigateAsync(AppPage.Browse));
                window.UpdateLayout();

                return (scrolled, viewer.VerticalOffset, top, list.Items[(int)viewer.VerticalOffset]);
            }
            finally
            {
                window.Close();
            }
        });

        // Then the list is where it was left, on the very same row
        Assert.Equal(250, before);
        Assert.Equal(before, after);
        Assert.Same(topBefore, topAfter);
    }

    [Fact]
    public void BrowserRow_ShouldAnnounceTheServerNameToScreenReaders()
    {
        // Given the Browse page with rows
        var (announced, expected) = RunOnSta(() =>
        {
            var (window, vm) = ShowBrowse("en");

            try
            {
                // When a row is read through its accessible name
                var item = vm.Browser.Servers[0];
                var row = RealizedRow(BrowsePage(window), item);
                return (System.Windows.Automation.AutomationProperties.GetName(row), item.Name);
            }
            finally
            {
                window.Close();
            }
        });

        // Then it is the server name, not the row type
        Assert.Equal(expected, announced);
    }

    [Fact]
    public void BrowsePage_Tab_ShouldReachTheRowsConnectAndSaveButtons()
    {
        // Given the Browse page with keyboard focus on the first row
        var focusPath = RunOnSta(() =>
        {
            var (window, vm) = ShowBrowse("en", saveLongServer: false);

            try
            {
                var page = BrowsePage(window);
                var row = RealizedRow(page, vm.Browser.Servers[0]);
                row.Focus();
                var path = new List<string>();

                // When Tab is pressed twice
                for (var i = 0; i < 2; i++)
                {
                    ((UIElement)Keyboard.FocusedElement!).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    path.Add(Keyboard.FocusedElement is Button button && IsInside(row, button)
                        ? button.Content as string ?? System.Windows.Automation.AutomationProperties.GetName(button)
                        : Keyboard.FocusedElement?.GetType().Name ?? "nothing");
                }

                return path;
            }
            finally
            {
                window.Close();
            }
        });

        // Then focus walks the row's Connect, then Save
        Assert.Equal(["Connect", "Save"], focusPath);
    }

    public enum BrowseState
    {
        Loading,
        Failed,
        NoMatch,
        Rows
    }

    [Theory]
    [InlineData(BrowseState.Loading, "LoadingState")]
    [InlineData(BrowseState.Failed, "FailedState")]
    [InlineData(BrowseState.NoMatch, "NoMatchState")]
    [InlineData(BrowseState.Rows, null)]
    public void BrowsePage_EachListState_ShouldShowOnlyItsOwnPanel(BrowseState state, string? expectedPanel)
    {
        // Given the Browse page in the given state
        var shown = RunOnSta(() =>
        {
            var handler = state switch
            {
                BrowseState.Loading => new NeverAnsweringHandler(),
                BrowseState.Failed => (HttpMessageHandler)new FakeHttpMessageHandler(true),
                _ => null
            };
            var (window, vm) = ShowBrowse("en", catalogHandler: handler);

            try
            {
                if (state == BrowseState.NoMatch)
                {
                    vm.Browser.HideEmpty = true;
                    vm.Browser.HideFull = true;
                    vm.Browser.SelectedGameFilterOption = vm.Browser.GameFilterOptions[3];
                    vm.Browser.SearchText = "no such server";
                    PumpUntil(() => vm.Browser.IsNoMatch);
                }

                window.UpdateLayout();
                var page = BrowsePage(window);

                // When the state panels and the list are inspected
                var panels = new[] { "LoadingState", "FailedState", "NoMatchState" }
                    .Where(name => ((FrameworkElement)page.FindName(name)).IsVisible)
                    .ToList();
                var rowsShown = Descendants<ListBoxItem>((DependencyObject)page.FindName("BrowserList")).Any(r => r.IsVisible);
                return (Panels: panels, RowsShown: rowsShown);
            }
            finally
            {
                window.Close();
            }
        });

        // Then exactly that state is drawn (and rows only when there are rows to show)
        Assert.Equal(expectedPanel is null ? [] : [expectedPanel], shown.Panels);
        Assert.Equal(state == BrowseState.Rows, shown.RowsShown);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BrowserRow_OnHover_ShouldEaseItsHoverLayerOnlyWithMotionOn(bool motion)
    {
        // Given a browser row hosted in a rendering window, with motion on or off
        var (animated, settled) = RunOnSta(() =>
        {
            var row = new ListBoxItem { Style = (Style)new ServerBrowser().Resources["BrowserRowStyle"], Content = "row" };
            var host = new Window
            {
                Content = row,
                Width = 300,
                Height = 100,
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000
            };
            Motion.SetIsEnabled(host, motion);
            host.Show();
            var layer = (Border)row.Template.FindName("HoverLayer", row);

            try
            {
                // When the pointer enters the row (the input system owns IsMouseOver)
                var key = (DependencyPropertyKey)typeof(UIElement)
                    .GetField("IsMouseOverPropertyKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .GetValue(null)!;
                row.SetValue(key, true);
                var easing = layer.HasAnimatedProperties;
                Views.StaThread.Pump(400);
                return (easing, layer.Opacity);
            }
            finally
            {
                host.Close();
            }
        });

        // Then with motion the layer eases in; without it snaps; both settle on the hover tint
        Assert.Equal(motion, animated);
        Assert.Equal(0.04, settled, 3);
    }

    private static void PumpUntil(Func<bool> done)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (!done() && DateTime.UtcNow < deadline)
        {
            Views.StaThread.Pump(5);
        }
    }

    // A catalog download that never answers: the page stays in its loading state.
    internal sealed class NeverAnsweringHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static ServerBrowser BrowsePage(MainWindow window) =>
        (ServerBrowser)((MainView)window.FindName("ShellContent")).FindName("BrowsePage");

    private static ListBoxItem RealizedRow(ServerBrowser page, ServerBrowserItem item)
    {
        var list = (ListBox)page.FindName("BrowserList");
        list.ScrollIntoView(item);
        list.UpdateLayout();
        return (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(item);
    }

    private static bool IsInside(DependencyObject ancestor, DependencyObject node)
    {
        for (DependencyObject? current = node; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in Descendants<T>(child))
            {
                yield return nested;
            }
        }
    }

    // The window on the Browse page over a fake catalog. Runs with a dispatcher sync context
    // so the load's continuations come back to this UI thread, as in the app.
    private static (MainWindow Window, MainViewModel ViewModel) ShowBrowse(
        string languageTag,
        int extraRows = 3,
        bool saveLongServer = true,
        HttpMessageHandler? catalogHandler = null,
        IEnumerable<Master.Server>? rows = null)
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
        var localizer = Localizer.FromEmbeddedResources();
        LocalizationSource.Instance.Attach(localizer);

        var entries = rows?.ToList() ?? new List<Master.Server>
        {
            ServerBrowserViewModelTests.Entry("zzzzzz", LongServerName, game: "gta5enhanced", players: 2048, max: 2048,
                endpoint: "203.0.113.200:30120")
        };

        for (var i = 0; rows is null && i < extraRows; i++)
        {
            entries.Add(ServerBrowserViewModelTests.Entry($"s{i:D5}", $"Server {i}", game: i % 2 == 0 ? "gta5" : "rdr3",
                players: i % 64, max: 64, endpoint: $"10.{i / 65536 % 256}.{i / 256 % 256}.{i % 256}:30120"));
        }

        var repository = new InMemoryServerRepository();

        if (saveLongServer)
        {
            repository.Add(SavedServer.Create("LSRP", "203.0.113.200:30120"));
        }

        var browser = new ServerBrowserViewModel(
            new ServerCatalog(new HttpClient(catalogHandler ?? new FakeHttpMessageHandler(
                HttpStatusCode.OK, ServerBrowserViewModelTests.CatalogBytes([.. entries])))),
            new FakeServerEnrichmentService(),
            repository,
            localizer);
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            browser: browser,
            localizer: localizer);
        PumpUntilDone(vm.InitializeAsync());
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
        var navigation = vm.NavigateAsync(AppPage.Browse);

        if (catalogHandler is NeverAnsweringHandler)
        {
            Views.StaThread.Pump(50);
        }
        else
        {
            PumpUntilDone(navigation);
        }

        window.UpdateLayout();
        return (window, vm);
    }

    private static void PumpUntilDone(Task task)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            Views.StaThread.Pump(5);
        }

        task.GetAwaiter().GetResult();
    }
}
