using System.Windows;
using System.Windows.Controls;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

// The Servers page at the minimum window size (1000×640), in every shipped language, with
// long names, the longest game build label and both requirements: nothing is clipped. Only
// the server names may shorten (with an ellipsis, by design); every other text is whole.
public partial class MainViewModelTests
{
    private const string LongServerName =
        "Los Santos Ultimate Roleplay | Economy | Jobs | Gangs | Custom Cars | Discord Required";

    private const string LongServerName2 =
        "Wild West Frontier Roleplay — Serious RP — Whitelisted — New Players Welcome";

    [Theory]
    [MemberData(nameof(ShippedLanguageTags))]
    public void ServersPage_AtMinimumSize_ShouldClipNothing(string languageTag)
    {
        // Given the real window at its minimum size, in the given language, with a long
        // listed server selected (longest build label, Steam and Discord required)
        var (problems, layout) = RunOnSta(() =>
        {
            var (window, vm) = ShowServersPage(languageTag);

            try
            {
                var shell = (MainView)window.FindName("ShellContent");
                var page = (ServersPage)shell.FindName("ServersPage");
                var list = page.FindName("ListColumn") is ColumnDefinition column
                    ? column.ActualWidth
                    : 0;

                // When the page is measured
                var problems = RegionProblems(window, "servers page", page)
                    .Where(problem => !problem.Contains(LongServerName) && !problem.Contains(LongServerName2))
                    .ToList();

                foreach (var name in new[] { "DetailBuild", "DetailPresence", "ConnectButton", "FilterChips", "RequirementChips" })
                {
                    if (page.FindName(name) is FrameworkElement { IsVisible: false })
                    {
                        problems.Add($"{name} is not shown");
                    }
                }

                return (problems, list);
            }
            finally
            {
                window.Close();
            }
        });

        // Then every element is whole, and the list column is about 340 px
        Assert.Empty(problems);
        Assert.InRange(layout, 320, 360);
    }

    [Fact]
    public void ServersPage_WhenLarger_ShouldStretchBothColumns()
    {
        // Given the page at its minimum size, then at a maximized 1920×1040
        var (small, large) = RunOnSta(() =>
        {
            var (window, _) = ShowServersPage("en");

            try
            {
                var shell = (MainView)window.FindName("ShellContent");
                var page = (ServersPage)shell.FindName("ServersPage");
                var list = (ColumnDefinition)page.FindName("ListColumn");
                var detail = (FrameworkElement)page.FindName("DetailPane");
                var atMinimum = (list.ActualWidth, detail.ActualWidth);

                // When the window grows
                window.Width = 1920;
                window.Height = 1040;
                window.UpdateLayout();

                return (atMinimum, (list.ActualWidth, detail.ActualWidth));
            }
            finally
            {
                window.Close();
            }
        });

        // Then both columns grow (the list up to its cap), the detail takes the rest
        Assert.True(large.Item1 > small.Item1, $"list {small.Item1:0} → {large.Item1:0}");
        Assert.True(large.Item2 > small.Item2, $"detail {small.Item2:0} → {large.Item2:0}");
        Assert.True(large.Item1 <= 460.5);
    }

    private static (MainWindow Window, MainViewModel ViewModel) ShowServersPage(string languageTag)
    {
        var localizer = Localizer.FromEmbeddedResources();
        LocalizationSource.Instance.Attach(localizer);
        var longestBuild = GameBuilds.Baseline.Names
            .MaxBy(entry => $"{entry.Key} · {entry.Value}".Length).Key;
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create(LongServerName, "cfx.re/join/aaaaaa", requiresSteam: true, requiresDiscord: true));
        repository.Add(SavedServer.Create(LongServerName2, "cfx.re/join/bbbbbb"));
        repository.Add(SavedServer.Create("WW", "play.a-rather-long-domain-name-for-a-server.example.com:30120"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence>
            {
                ["aaaaaa"] = new(true, 1024, 2048, GameClient.FiveMEnhanced, GameBuild: longestBuild, PureMode: 2),
                ["bbbbbb"] = new(true, 12, 48, GameClient.RedM)
            }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment, localizer: localizer);
        vm.InitializeAsync().GetAwaiter().GetResult();
        vm.RefreshServerInfoAsync().GetAwaiter().GetResult();

        // After the view model: it applies the saved language on construction.
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
}
