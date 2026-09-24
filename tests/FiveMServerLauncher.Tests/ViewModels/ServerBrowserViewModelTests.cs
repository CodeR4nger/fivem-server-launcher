using System.Net;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Service;
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

    private static ServerBrowserViewModel CreateViewModel(
        byte[] catalogBytes,
        FakeServerEnrichmentService? enrichment = null,
        TimeSpan? refreshCooldown = null,
        IServerRepository? repository = null)
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, catalogBytes);
        return new ServerBrowserViewModel(
            new ServerCatalog(new HttpClient(handler)),
            enrichment ?? new FakeServerEnrichmentService(),
            repository ?? new InMemoryServerRepository(),
            refreshCooldown);
    }

    private static ServerBrowserViewModel CreateViewModel(HttpClient httpClient)
    {
        return new ServerBrowserViewModel(
            new ServerCatalog(httpClient),
            new FakeServerEnrichmentService(),
            new InMemoryServerRepository());
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
        var vm = CreateViewModel(
            CatalogBytes(Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32)),
            enrichment: enrichment);
        await vm.LoadAsync();
        var row = vm.Servers[0];

        // When — accessing the icon starts a single background load
        _ = row.Icon;
        _ = row.Icon;
        await Task.Delay(50);

        // Then
        Assert.Equal(1, enrichment.IconCalls);
        Assert.Equal(new byte[] { 9, 8, 7 }, row.Icon);
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
        server.Data.Vars["iconVersion"] = "55";
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
