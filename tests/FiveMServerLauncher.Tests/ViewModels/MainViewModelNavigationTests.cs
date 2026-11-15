using System.ComponentModel;
using System.Net;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Localization;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

// Page navigation: one CurrentPage (Servers, Browse, Settings) is the whole navigation
// state; the browser view model lives across page switches.
public partial class MainViewModelTests
{
    [Fact]
    public void CurrentPage_ShouldStartOnServers()
    {
        // Given / When
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // Then
        Assert.Equal(AppPage.Servers, vm.CurrentPage);
        Assert.True(vm.IsServersPage);
        Assert.False(vm.IsBrowsePage);
        Assert.False(vm.IsSettingsPage);
    }

    [Theory]
    [InlineData(AppPage.Servers)]
    [InlineData(AppPage.Browse)]
    [InlineData(AppPage.Settings)]
    public async Task NavigateCommand_ShouldShowOnlyTheRequestedPage(AppPage page)
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        await vm.InitializeAsync();

        // When
        vm.NavigateCommand.Execute(page);
        await vm.NavigateAsync(page);

        // Then
        Assert.Equal(page, vm.CurrentPage);
        Assert.Equal(page == AppPage.Servers, vm.IsServersPage);
        Assert.Equal(page == AppPage.Browse, vm.IsBrowsePage);
        Assert.Equal(page == AppPage.Settings, vm.IsSettingsPage);
    }

    [Fact]
    public async Task NavigateAsync_ShouldRaiseChangeForThePageAndEveryPageFlag()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        await vm.NavigateAsync(AppPage.Settings);

        // Then
        Assert.Contains(nameof(MainViewModel.CurrentPage), raised);
        Assert.Contains(nameof(MainViewModel.IsServersPage), raised);
        Assert.Contains(nameof(MainViewModel.IsSettingsPage), raised);
        Assert.Contains(nameof(MainViewModel.IsBrowsePage), raised);
    }

    [Fact]
    public async Task NavigateAsync_ToTheCurrentPage_ShouldRaiseNothing()
    {
        // Given
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        await vm.NavigateAsync(AppPage.Servers);

        // Then
        Assert.Empty(raised);
    }

    [Fact]
    public async Task NavigateAsync_ToBrowseFirstTime_ShouldLoadTheCatalog()
    {
        // Given
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BrowserCatalog());
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), browser: Browser(handler));

        // When
        await vm.NavigateAsync(AppPage.Browse);

        // Then
        Assert.Equal(2, vm.Browser.Servers.Count);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task NavigateAsync_BackToBrowse_ShouldKeepFiltersSortSearchAndRows()
    {
        // Given — the browser visited once and narrowed down
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, BrowserCatalog());
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), browser: Browser(handler));
        await vm.NavigateAsync(AppPage.Browse);
        var browser = vm.Browser;
        browser.HideFull = true;
        browser.SelectedSortOption = browser.SortOptions[^1];
        browser.SearchText = "alp";
        var rows = browser.Servers;

        // When — away to another page and back
        await vm.NavigateAsync(AppPage.Servers);
        await vm.NavigateAsync(AppPage.Settings);
        await vm.NavigateAsync(AppPage.Browse);

        // Then — the same view model and the same rows (so the scroll position too),
        // with no second catalog load
        Assert.Same(browser, vm.Browser);
        Assert.Same(rows, vm.Browser.Servers);
        Assert.True(browser.HideFull);
        Assert.Same(browser.SortOptions[^1], browser.SelectedSortOption);
        Assert.Equal("alp", browser.SearchText);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task NavigateAsync_ToBrowseAfterAFailedLoad_ShouldTryAgain()
    {
        // Given — the first visit hit an outage
        var handler = new FailOnceHandler(BrowserCatalog());
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), browser: Browser(handler));
        await vm.NavigateAsync(AppPage.Browse);
        Assert.True(vm.Browser.LoadFailed);
        await vm.NavigateAsync(AppPage.Servers);

        // When
        await vm.NavigateAsync(AppPage.Browse);

        // Then
        Assert.False(vm.Browser.LoadFailed);
        Assert.Equal(2, vm.Browser.Servers.Count);
    }

    private static byte[] BrowserCatalog() => ServerBrowserViewModelTests.CatalogBytes(
        ServerBrowserViewModelTests.Entry("aaaaaa", "Alpha", game: "gta5", players: 3, max: 32),
        ServerBrowserViewModelTests.Entry("bbbbbb", "Beta", game: "rdr3", players: 64, max: 64));

    private static ServerBrowserViewModel Browser(HttpMessageHandler handler) => new(
        new ServerCatalog(new HttpClient(handler)),
        new FakeServerEnrichmentService(),
        new InMemoryServerRepository(),
        TestLocalizer.English());

    private sealed class FailOnceHandler(byte[] content) : HttpMessageHandler
    {
        private bool _failed;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_failed)
            {
                _failed = true;
                throw new HttpRequestException("Simulated outage");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
        }
    }
}
