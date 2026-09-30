using System.Net;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.Tests.Localization;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

public class ServerBrowserViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldMapSnapshotEntries()
    {
        // Given
        var vm = CreateViewModel(CatalogBytes(
            Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32),
            Entry("bbbbbb", "Beta", game: "rdr3", players: 0, max: 64)));

        // When
        await vm.LoadAsync();

        // Then
        Assert.Equal(2, vm.Servers.Count);
        Assert.False(vm.LoadFailed);
        Assert.Equal("Alpha", vm.Servers[0].Name);
        Assert.Equal(GameClient.FiveM, vm.Servers[0].Game);
    }

    [Fact]
    public async Task LoadAsync_WhenEntryHasNoData_ShouldSkipIt()
    {
        // Given
        var vm = CreateViewModel(CatalogBytes(
            new Master.Server { EndPoint = "nodata" },
            Entry("aaaaaa", "Alpha", game: "gta5", players: 1, max: 32)));

        // When
        await vm.LoadAsync();

        // Then
        Assert.Single(vm.Servers);
    }

    [Fact]
    public async Task LoadAsync_WhenOutage_ShouldSetLoadFailedAndKeepEmpty()
    {
        // Given — handler that throws on send
        var vm = CreateViewModel(new HttpClient(new FakeHttpMessageHandler(true)));

        // When
        await vm.LoadAsync();

        // Then
        Assert.True(vm.LoadFailed);
        Assert.Empty(vm.Servers);
    }

    [Fact]
    public async Task GameFilter_ShouldRestrictToSelectedGame()
    {
        // Given
        var vm = CreateViewModel(CatalogBytes(
            Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32),
            Entry("bbbbbb", "Beta", game: "rdr3", players: 3, max: 32),
            Entry("cccccc", "Gamma", game: "gta5enhanced", players: 3, max: 32)));
        await vm.LoadAsync();

        // When
        vm.GameFilter = GameClient.RedM;

        // Then
        var visible = vm.ServersView.Cast<ServerBrowserItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Beta", visible[0].Name);

        // And All (null) restores everything
        vm.GameFilter = null;
        Assert.Equal(3, vm.ServersView.Cast<ServerBrowserItem>().Count());
    }

    [Fact]
    public async Task HideFull_ShouldExcludeFullServers()
    {
        // Given
        var vm = CreateViewModel(CatalogBytes(
            Entry("aaaaaa", "Alpha", game: "gta5", players: 32, max: 32),
            Entry("bbbbbb", "Beta", game: "gta5", players: 5, max: 32)));
        await vm.LoadAsync();

        // When
        vm.HideFull = true;

        // Then
        var visible = vm.ServersView.Cast<ServerBrowserItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Beta", visible[0].Name);
    }

    [Fact]
    public async Task HideEmpty_ShouldExcludeEmptyServers()
    {
        // Given
        var vm = CreateViewModel(CatalogBytes(
            Entry("aaaaaa", "Alpha", game: "gta5", players: 0, max: 32),
            Entry("bbbbbb", "Beta", game: "gta5", players: 5, max: 32)));
        await vm.LoadAsync();

        // When
        vm.HideEmpty = true;

        // Then
        var visible = vm.ServersView.Cast<ServerBrowserItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Beta", visible[0].Name);
    }

    [Fact]
    public async Task SearchText_ShouldFilterByNameCaseInsensitiveAndCombine()
    {
        // Given
        var vm = CreateViewModel(CatalogBytes(
            Entry("aaaaaa", "Midnight RP", game: "gta5", players: 0, max: 32),
            Entry("bbbbbb", "Midnight Drift", game: "rdr3", players: 9, max: 32),
            Entry("cccccc", "Daylight", game: "gta5", players: 2, max: 32)));
        await vm.LoadAsync();

        // When — search + hide-empty + game all combine (AND)
        vm.SearchText = "midnight";
        vm.HideEmpty = true;
        vm.GameFilter = GameClient.RedM;

        // Then
        var visible = vm.ServersView.Cast<ServerBrowserItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Midnight Drift", visible[0].Name);

        // Clearing the search widens but keeps other filters
        vm.SearchText = "";
        Assert.Single(vm.ServersView.Cast<ServerBrowserItem>());
    }

    [Fact]
    public async Task SearchText_WhenDebouncePending_ShouldNotRefreshUntilElapsed()
    {
        // Given — refreshing the 30k-row view per keystroke freezes the UI; the
        // refresh only runs once the searcher stops typing.
        var delay = new ManualSearchDelay();
        var vm = CreateViewModel(
            CatalogBytes(
                Entry("aaaaaa", "Midnight RP", game: "gta5", players: 0, max: 32),
                Entry("bbbbbb", "Daylight", game: "gta5", players: 2, max: 32)),
            searchDebounce: TimeSpan.FromMilliseconds(250),
            searchDelay: delay.Invoke);
        await vm.LoadAsync();

        // When
        vm.SearchText = "midnight";

        // Then — the debounce is pending, nothing filtered yet.
        Assert.Equal(2, vm.ServersView.Cast<ServerBrowserItem>().Count());
        var pending = Assert.Single(delay.Pending);
        Assert.Equal(TimeSpan.FromMilliseconds(250), pending.Duration);

        // When — the idle window elapses
        pending.Completion.TrySetResult();

        // Then
        var visible = vm.ServersView.Cast<ServerBrowserItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Midnight RP", visible[0].Name);
    }

    [Fact]
    public async Task SearchText_WhenKeystrokesArriveDuringDebounce_ShouldCoalesceToFinalText()
    {
        // Given — rapid keystrokes cancel each other's pending refresh; only the
        // final text ever filters the view ("drift" narrows to one row where the
        // intermediate "mid" would have shown two).
        var delay = new ManualSearchDelay();
        var vm = CreateViewModel(
            CatalogBytes(
                Entry("aaaaaa", "Midnight RP", game: "gta5", players: 0, max: 32),
                Entry("bbbbbb", "Midnight Drift", game: "gta5", players: 9, max: 32)),
            searchDebounce: TimeSpan.FromMilliseconds(250),
            searchDelay: delay.Invoke);
        await vm.LoadAsync();

        // When — two keystrokes inside one idle window
        vm.SearchText = "mid";
        vm.SearchText = "drift";

        // Then — the first debounce was cancelled by the second
        Assert.True(delay.Pending[0].Completion.Task.IsCanceled);

        // When — only the final debounce elapses
        Assert.True(delay.Pending[1].Completion.TrySetResult());

        // Then — filtered by the FINAL text only ("drift" narrows to one row).
        var visible = vm.ServersView.Cast<ServerBrowserItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Midnight Drift", visible[0].Name);
        Assert.Equal(2, delay.Pending.Count);
    }

    private sealed class ManualSearchDelay
    {
        private readonly List<(TimeSpan Duration, TaskCompletionSource Completion)> _pending = new();

        public IReadOnlyList<(TimeSpan Duration, TaskCompletionSource Completion)> Pending => _pending;

        public Func<TimeSpan, CancellationToken, Task> Invoke => (duration, token) =>
        {
            // Completing the delay runs the debounce continuation inline on the
            // completing thread (production re-posts to the dispatcher via the
            // captured sync context; the fake keeps the tests deterministic).
            var completion = new TaskCompletionSource();
            token.Register(() => completion.TrySetCanceled());
            _pending.Add((duration, completion));
            return completion.Task;
        };
    }

    private static ServerBrowserViewModel CreateViewModel(
        byte[] catalogBytes,
        FakeServerEnrichmentService? enrichment = null,
        TimeSpan? refreshCooldown = null,
        IServerRepository? repository = null,
        TimeSpan? searchDebounce = null,
        Func<TimeSpan, CancellationToken, Task>? searchDelay = null)
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, catalogBytes);
        return new ServerBrowserViewModel(
            new ServerCatalog(new HttpClient(handler)),
            enrichment ?? new FakeServerEnrichmentService(),
            repository ?? new InMemoryServerRepository(),
            TestLocalizer.English(),
            refreshCooldown,
            searchDebounce: searchDebounce ?? TimeSpan.Zero,
            searchDelay: searchDelay);
    }

    private static ServerBrowserViewModel CreateViewModel(HttpClient httpClient)
    {
        return new ServerBrowserViewModel(
            new ServerCatalog(httpClient),
            new FakeServerEnrichmentService(),
            new InMemoryServerRepository(),
            TestLocalizer.English());
    }

    [Fact]
    public void GameFilterOptions_WhenLanguageSwitched_ShouldLocalizeAllLabelAndKeepSelection()
    {
        // Given
        var localizer = new Localizer(
            Localizer.ParseDictionaries(
                ("en", """{ "GameFilterAll": "ALL", "GameFilterFiveM": "FIVEM", "GameFilterEnhanced": "ENHANCED", "GameFilterRedM": "REDM" }"""),
                ("es", """{ "GameFilterAll": "TODO", "GameFilterFiveM": "FIVEM", "GameFilterEnhanced": "ENHANCED", "GameFilterRedM": "REDM" }""")),
            () => "en-US");
        var vm = new ServerBrowserViewModel(
            new ServerCatalog(new HttpClient(new FakeHttpMessageHandler(true))),
            new FakeServerEnrichmentService(),
            new InMemoryServerRepository(),
            localizer: localizer);
        vm.SelectedGameFilterOption = vm.GameFilterOptions.Single(o => o.Game == GameClient.RedM);

        // When
        localizer.SetLanguage("es");

        // Then
        Assert.Equal("TODO", vm.GameFilterOptions[0].Label);
        Assert.Equal("REDM", vm.SelectedGameFilterOption.Label);
        Assert.Equal(GameClient.RedM, vm.SelectedGameFilterOption.Game);
        Assert.Equal(GameClient.RedM, vm.GameFilter);
    }

    [Fact]
    public async Task LoadAsync_WhenServerAlreadySaved_ShouldMarkRow()
    {
        // Given — saved by address (catalog endpoint) and by cfx id respectively
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Mine", "149.56.120.52:30120"));
        repository.Add(SavedServer.Create("Also Mine", "cfx.re/join/bbbbbb"));
        var vm = CreateViewModel(CatalogBytes(
            Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32, endpoint: "149.56.120.52:30120"),
            Entry("bbbbbb", "Beta", game: "gta5", players: 3, max: 32, endpoint: "9.9.9.9:30120"),
            Entry("cccccc", "Gamma", game: "gta5", players: 3, max: 32, endpoint: "8.8.8.8:30120")),
            repository: repository);

        // When
        await vm.LoadAsync();

        // Then
        Assert.True(vm.Servers[0].IsSaved);
        Assert.True(vm.Servers[1].IsSaved);
        Assert.False(vm.Servers[2].IsSaved);
    }


    [Fact]
    public async Task RefreshCommand_ShouldForceRefreshAndReloadRows()
    {
        // Given
        var enrichment = new FakeServerEnrichmentService();
        var vm = CreateViewModel(
            CatalogBytes(Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)),
            enrichment: enrichment,
            refreshCooldown: TimeSpan.Zero);
        await vm.LoadAsync();

        // When
        vm.RefreshCommand.Execute(null);
        await Task.Delay(50);

        // Then
        Assert.Equal(1, enrichment.ForcedRefreshCalls);
        Assert.Single(vm.Servers);
    }

    [Fact]
    public async Task RefreshCommand_WhileRunning_ShouldBeDisabled()
    {
        // Given
        var enrichment = new FakeServerEnrichmentService();
        var gate = new TaskCompletionSource();
        enrichment.RefreshDelay = gate.Task;
        var vm = CreateViewModel(
            CatalogBytes(Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)),
            enrichment: enrichment,
            refreshCooldown: TimeSpan.Zero);
        await vm.LoadAsync();

        // When
        vm.RefreshCommand.Execute(null);

        // Then
        Assert.False(vm.RefreshCommand.CanExecute(null));
        gate.SetResult();
        await Task.Delay(50);
        Assert.True(vm.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshCommand_AfterRefresh_ShouldStayDisabledForCooldown()
    {
        // Given
        var vm = CreateViewModel(
            CatalogBytes(Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)),
            enrichment: new FakeServerEnrichmentService(),
            refreshCooldown: TimeSpan.FromMilliseconds(400));
        await vm.LoadAsync();

        // When
        vm.RefreshCommand.Execute(null);
        await Task.Delay(150);

        // Then
        Assert.False(vm.RefreshCommand.CanExecute(null));
        await Task.Delay(400);
        Assert.True(vm.RefreshCommand.CanExecute(null));
    }

    [Fact]
    public async Task RefreshCommand_WhenOutage_ShouldKeepRows()
    {
        // Given — refresh succeeds on load, then the enrichment throws on the forced refresh
        var vm = CreateViewModel(
            CatalogBytes(Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)),
            enrichment: new FakeServerEnrichmentService { ThrowOnRefreshCount = 1 },
            refreshCooldown: TimeSpan.Zero);
        await vm.LoadAsync();

        // When
        vm.RefreshCommand.Execute(null);
        await Task.Delay(50);

        // Then
        Assert.Single(vm.Servers);
        Assert.False(vm.LoadFailed);
    }

    [Fact]
    public async Task RowIcon_ShouldLoadLazilyAndOnce()
    {
        // Given
        var enrichment = new FakeServerEnrichmentService { Icon = [9, 8, 7] };
        var server = Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32);
        server.Data.IconVersion = 55;
        var vm = CreateViewModel(CatalogBytes(server), enrichment: enrichment);
        await vm.LoadAsync();
        var row = vm.Servers[0];

        // When — accessing the icon starts a single background load
        _ = row.Icon;
        _ = row.Icon;
        await Task.Delay(50);

        // Then
        Assert.Equal(1, enrichment.DirectIconCalls);
        Assert.Equal(new byte[] { 9, 8, 7 }, row.Icon);
    }

    [Fact]
    public async Task RowIcon_WhenCatalogPublishesNoIconVersion_ShouldNotProbeForOne()
    {
        // Given — the snapshot is the proof: a server publishing no iconVersion has no
        // icon, so the row never falls back to a /single/ probe (request hygiene).
        var enrichment = new FakeServerEnrichmentService { Icon = [9, 8, 7] };
        var vm = CreateViewModel(
            CatalogBytes(Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)),
            enrichment: enrichment);
        await vm.LoadAsync();
        var row = vm.Servers[0];

        // When
        _ = row.Icon;
        await Task.Delay(50);

        // Then
        Assert.Equal(0, enrichment.DirectIconCalls);
        Assert.Null(row.Icon);
    }

    [Fact]
    public async Task SelectedGameFilterOption_ShouldDriveGameFilter()
    {
        // Given
        var vm = CreateViewModel(CatalogBytes(
            Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32),
            Entry("bbbbbb", "Beta", game: "rdr3", players: 3, max: 32)));
        await vm.LoadAsync();

        // When
        vm.SelectedGameFilterOption = vm.GameFilterOptions.First(o => o.Game == GameClient.RedM);

        // Then
        var visible = vm.ServersView.Cast<ServerBrowserItem>().ToList();
        Assert.Single(visible);
        Assert.Equal("Beta", visible[0].Name);

        // And "All" restores everything
        vm.SelectedGameFilterOption = vm.GameFilterOptions.First(o => o.Game is null);
        Assert.Equal(2, vm.ServersView.Cast<ServerBrowserItem>().Count());
    }

    [Fact]
    public async Task RowIcon_WhenCatalogPublishesIconVersion_ShouldUseItDirectly()
    {
        // Given
        var enrichment = new FakeServerEnrichmentService { Icon = [9, 8, 7] };
        var server = Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32);
        server.Data.IconVersion = 55;
        var vm = CreateViewModel(CatalogBytes(server), enrichment: enrichment);
        await vm.LoadAsync();
        var row = vm.Servers[0];

        // When
        _ = row.Icon;
        await Task.Delay(50);

        // Then
        Assert.Equal(1, enrichment.DirectIconCalls);
        Assert.Equal("55", enrichment.LastRequestedIconVersion);
        Assert.Equal(new byte[] { 9, 8, 7 }, row.Icon);
    }

    [Fact]
    public async Task LoadAsync_WhileFirstFetchInFlight_ShouldShowLoadingOverlay()
    {
        // Given a catalog download that blocks until released
        var handler = new GatedHttpMessageHandler(CatalogBytes(
            Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)));
        var vm = CreateViewModel(new HttpClient(handler));

        // When the browser opens and the fetch is still running
        var load = vm.LoadAsync();

        // Then the empty browser shows the loading state until the snapshot lands
        Assert.True(vm.IsRefreshing);
        Assert.True(vm.IsLoadingOverlay);

        handler.Release();
        await load;

        Assert.False(vm.IsRefreshing);
        Assert.False(vm.IsLoadingOverlay);
        Assert.Single(vm.Servers);
    }

    [Fact]
    public async Task LoadAsync_WhenRefreshingWithRowsPresent_ShouldKeepLoadingOverlayHidden()
    {
        // Given rows from a previous load and a manual refresh held mid-flight
        var gate = new TaskCompletionSource();
        var enrichment = new FakeServerEnrichmentService { RefreshDelay = gate.Task };
        var vm = CreateViewModel(
            CatalogBytes(Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)),
            enrichment,
            refreshCooldown: TimeSpan.Zero);
        await vm.LoadAsync();
        Assert.Single(vm.Servers);

        // When the manual refresh runs behind the gate
        var refresh = vm.RefreshAsync();

        // Then the refreshing flag is up but the overlay stays hidden behind the rows
        Assert.True(vm.IsRefreshing);
        Assert.False(vm.IsLoadingOverlay);

        gate.SetResult();
        await refresh;

        Assert.False(vm.IsRefreshing);
        Assert.False(vm.IsLoadingOverlay);
    }

    [Fact]
    public async Task LoadAsync_WhenOutage_ShouldClearLoadingStateAndShowFailure()
    {
        // Given a failing catalog
        var vm = CreateViewModel(new HttpClient(new FakeHttpMessageHandler(true)));

        // When the browser opens
        await vm.LoadAsync();

        // Then the failure state stands alone — no loading overlay, no stuck flag
        Assert.True(vm.LoadFailed);
        Assert.False(vm.IsLoadingOverlay);
        Assert.False(vm.IsRefreshing);
        Assert.Empty(vm.Servers);
    }

    private sealed class GatedHttpMessageHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _gate = new();
        private readonly byte[] _response;

        public GatedHttpMessageHandler(byte[] response)
        {
            _response = response;
        }

        public void Release()
        {
            _gate.TrySetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _gate.Task;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(_response)
            };
        }
    }

    private static byte[] CatalogBytes(params Master.Server[] servers)
    {
        return TestProtobufFrames.Join(servers.Select(TestProtobufFrames.BuildFrameStream).ToArray());
    }

    private static Master.Server Entry(string id, string name, string game, int players, int max, string? endpoint = null)
    {
        return new Master.Server
        {
            EndPoint = id,
            Data = new Master.ServerData
            {
                Clients = players,
                SvMaxclients = max,
                Vars =
                {
                    ["sv_projectName"] = name,
                    ["gamename"] = game
                },
                ConnectEndPoints = { endpoint ?? $"10.0.0.{(byte)id[0]}:30120" }
            }
        };
    }
}
