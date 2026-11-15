using System.Net;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Localization;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

public class ServerBrowserSortingTests
{
    [Fact]
    public async Task LoadAsync_ShouldSortByMostPlayersFirstByDefault()
    {
        // Given
        var vm = CreateViewModel(
            Entry("aaaaaa", "Small", players: 2),
            Entry("bbbbbb", "Big", players: 90),
            Entry("cccccc", "Medium", players: 40));

        // When
        await vm.LoadAsync();

        // Then
        Assert.Equal(ServerBrowserSort.PlayersDescending, vm.SelectedSortOption.Sort);
        Assert.Equal(["Big", "Medium", "Small"], VisibleNames(vm));
    }

    [Theory]
    [InlineData(ServerBrowserSort.PlayersDescending, new[] { "Charlie", "alpha", "Bravo" })]
    [InlineData(ServerBrowserSort.PlayersAscending, new[] { "Bravo", "alpha", "Charlie" })]
    [InlineData(ServerBrowserSort.NameAscending, new[] { "alpha", "Bravo", "Charlie" })]
    [InlineData(ServerBrowserSort.NameDescending, new[] { "Charlie", "Bravo", "alpha" })]
    public async Task SelectedSortOption_ShouldOrderRowsByKeyAndDirection(ServerBrowserSort sort, string[] expected)
    {
        // Given
        var vm = CreateViewModel(
            Entry("aaaaaa", "Bravo", players: 1),
            Entry("bbbbbb", "Charlie", players: 50),
            Entry("cccccc", "alpha", players: 10));
        await vm.LoadAsync();

        // When
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == sort);

        // Then
        Assert.Equal(expected, VisibleNames(vm));
    }

    [Theory]
    [InlineData(ServerBrowserSort.PlayersDescending)]
    [InlineData(ServerBrowserSort.PlayersAscending)]
    public async Task PlayersSort_WhenPlayersTie_ShouldBreakTiesByNameThenCfxIdAscending(ServerBrowserSort sort)
    {
        // Given — equal players; two rows share a name too
        var vm = CreateViewModel(
            Entry("zzzzzz", "Same", players: 5),
            Entry("bbbbbb", "Delta", players: 5),
            Entry("aaaaaa", "Same", players: 5));
        await vm.LoadAsync();

        // When
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == sort);

        // Then — whatever the direction, tie-breakers stay ascending
        Assert.Equal(["bbbbbb", "aaaaaa", "zzzzzz"], VisibleIds(vm));
    }

    [Fact]
    public async Task NameSort_WhenNamesTie_ShouldBreakTiesByCfxId()
    {
        // Given — names equal under the case-insensitive compare
        var vm = CreateViewModel(
            Entry("cccccc", "same", players: 1),
            Entry("aaaaaa", "SAME", players: 9),
            Entry("bbbbbb", "Same", players: 4));
        await vm.LoadAsync();

        // When
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == ServerBrowserSort.NameDescending);

        // Then
        Assert.Equal(["aaaaaa", "bbbbbb", "cccccc"], VisibleIds(vm));
    }

    [Fact]
    public async Task NameSort_ShouldIgnoreColorCodesSymbolsEmojiAndWhitespace()
    {
        // Given — decorated names sort by their letters; a name with none sorts last
        var vm = CreateViewModel(
            Entry("aaaaaa", "^1Charlie", players: 1),
            Entry("bbbbbb", "★ [EU] Bravo", players: 1),
            Entry("cccccc", "\U0001F525 alpha", players: 1),
            Entry("dddddd", "  ★★★  ", players: 1),
            Entry("eeeeee", "Delta", players: 1));
        await vm.LoadAsync();

        // When
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == ServerBrowserSort.NameAscending);

        // Then — "[EU] Bravo" keys as "EUBravo", so it lands after Delta
        Assert.Equal(["cccccc", "aaaaaa", "eeeeee", "bbbbbb", "dddddd"], VisibleIds(vm));
    }

    [Theory]
    [InlineData(ServerBrowserSort.NameAscending, new[] { "aaaaaa", "bbbbbb", "cccccc", "dddddd" })]
    [InlineData(ServerBrowserSort.NameDescending, new[] { "bbbbbb", "aaaaaa", "cccccc", "dddddd" })]
    public async Task NameSort_WhenNameHasNoLettersOrDigits_ShouldSortItLastInBothDirections(
        ServerBrowserSort sort,
        string[] expected)
    {
        // Given — blank-looking names would otherwise fill the top of NAME A–Z
        var vm = CreateViewModel(
            Entry("dddddd", "???", players: 1),
            Entry("bbbbbb", "Bravo", players: 1),
            Entry("cccccc", "~*", players: 1),
            Entry("aaaaaa", "Alpha", players: 1));
        await vm.LoadAsync();

        // When
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == sort);

        // Then — letterless names keep their cfx-id order among themselves
        Assert.Equal(expected, VisibleIds(vm));
    }

    [Fact]
    public async Task Sort_ShouldComposeWithFiltersAndSearch()
    {
        // Given
        var vm = CreateViewModel(
            Entry("aaaaaa", "Midnight A", players: 3, game: "gta5"),
            Entry("bbbbbb", "Midnight B", players: 30, game: "gta5"),
            Entry("cccccc", "Midnight C", players: 0, game: "gta5"),
            Entry("dddddd", "Midnight D", players: 50, game: "rdr3"),
            Entry("eeeeee", "Daylight", players: 99, game: "gta5"));
        await vm.LoadAsync();
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == ServerBrowserSort.PlayersAscending);

        // When
        vm.GameFilter = GameClient.FiveM;
        vm.HideEmpty = true;
        vm.SearchText = "midnight";
        await Task.Yield();

        // Then
        Assert.Equal(["Midnight A", "Midnight B"], VisibleNames(vm));

        // And changing the sort keeps the filters
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == ServerBrowserSort.PlayersDescending);
        Assert.Equal(["Midnight B", "Midnight A"], VisibleNames(vm));
    }

    [Fact]
    public async Task LoadAsync_AfterSortChange_ShouldKeepTheSelectedSort()
    {
        // Given
        var vm = CreateViewModel(
            Entry("aaaaaa", "Bravo", players: 1),
            Entry("bbbbbb", "Alpha", players: 50));
        await vm.LoadAsync();
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == ServerBrowserSort.NameDescending);

        // When — the refresh replaces the collection wholesale
        await vm.LoadAsync();

        // Then
        Assert.Equal(["Bravo", "Alpha"], VisibleNames(vm));
        Assert.Equal(["Bravo", "Alpha"], vm.Servers.Select(s => s.Name));
    }

    [Fact]
    public async Task SortChange_ShouldKeepRowStateAndRaiseViewChange()
    {
        // Given
        var vm = CreateViewModel(
            Entry("aaaaaa", "Bravo", players: 1),
            Entry("bbbbbb", "Alpha", players: 50));
        await vm.LoadAsync();
        var rows = vm.Servers.ToHashSet();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == ServerBrowserSort.NameAscending);

        // Then — same row instances (icons/saved markers survive), the view is re-announced
        Assert.True(rows.SetEquals(vm.Servers));
        Assert.Contains(nameof(ServerBrowserViewModel.ServersView), raised);
        Assert.Contains(nameof(ServerBrowserViewModel.SelectedSortOption), raised);
    }

    [Fact]
    public void SortOptions_WhenLanguageSwitched_ShouldLocalizeLabelsAndKeepSelection()
    {
        // Given
        var localizer = new Localizer(
            Localizer.ParseDictionaries(
                ("en", """{ "SortMostPlayers": "MOST PLAYERS", "SortFewestPlayers": "FEWEST PLAYERS", "SortNameAscending": "NAME A-Z", "SortNameDescending": "NAME Z-A" }"""),
                ("es", """{ "SortMostPlayers": "MÁS JUGADORES", "SortFewestPlayers": "MENOS JUGADORES", "SortNameAscending": "NOMBRE A-Z", "SortNameDescending": "NOMBRE Z-A" }""")),
            () => "en-US");
        var vm = new ServerBrowserViewModel(
            new ServerCatalog(new HttpClient(new FakeHttpMessageHandler(true))),
            new FakeServerEnrichmentService(),
            new InMemoryServerRepository(),
            localizer: localizer);
        vm.SelectedSortOption = vm.SortOptions.Single(o => o.Sort == ServerBrowserSort.NameDescending);
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        localizer.SetLanguage("es");

        // Then
        Assert.Equal(
            ["MÁS JUGADORES", "MENOS JUGADORES", "NOMBRE A-Z", "NOMBRE Z-A"],
            vm.SortOptions.Select(o => o.Label));
        Assert.Equal("NOMBRE Z-A", vm.SelectedSortOption.Label);
        Assert.Equal(ServerBrowserSort.NameDescending, vm.SelectedSortOption.Sort);
        Assert.Contains(nameof(ServerBrowserViewModel.SortOptions), raised);
        Assert.Contains(nameof(ServerBrowserViewModel.SelectedSortOption), raised);
    }

    [Fact]
    public async Task SelectedSortOption_WhenSetToNull_ShouldKeepTheCurrentSort()
    {
        // Given — WPF may push null through the SelectedItem binding while ItemsSource is swapped
        var vm = CreateViewModel(
            Entry("aaaaaa", "Bravo", players: 1),
            Entry("bbbbbb", "Alpha", players: 50));
        await vm.LoadAsync();

        // When
        vm.SelectedSortOption = null!;

        // Then
        Assert.Equal(ServerBrowserSort.PlayersDescending, vm.SelectedSortOption.Sort);
        Assert.Equal(["Alpha", "Bravo"], VisibleNames(vm));
    }

    private static string[] VisibleNames(ServerBrowserViewModel vm)
    {
        return vm.ServersView.Cast<ServerBrowserItem>().Select(s => s.Name).ToArray();
    }

    private static string[] VisibleIds(ServerBrowserViewModel vm)
    {
        return vm.ServersView.Cast<ServerBrowserItem>().Select(s => s.CfxId).ToArray();
    }

    private static ServerBrowserViewModel CreateViewModel(params Master.Server[] servers)
    {
        var bytes = TestProtobufFrames.Join(servers.Select(TestProtobufFrames.BuildFrameStream).ToArray());
        return new ServerBrowserViewModel(
            new ServerCatalog(new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, bytes))),
            new FakeServerEnrichmentService(),
            new InMemoryServerRepository(),
            TestLocalizer.English(),
            searchDebounce: TimeSpan.Zero,
            searchDelay: (_, _) => Task.CompletedTask);
    }

    private static Master.Server Entry(string id, string name, int players, string game = "gta5")
    {
        return new Master.Server
        {
            EndPoint = id,
            Data = new Master.ServerData
            {
                Clients = players,
                SvMaxclients = 128,
                Vars =
                {
                    ["sv_projectName"] = name,
                    ["gamename"] = game
                },
                ConnectEndPoints = { $"10.0.0.{(byte)id[0]}:30120" }
            }
        };
    }
}
