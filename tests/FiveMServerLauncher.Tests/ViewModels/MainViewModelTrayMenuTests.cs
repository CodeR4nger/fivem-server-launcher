using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;
using Xunit;

namespace FiveMServerLauncher.Tests.ViewModels;

// The tray menu is rebuilt from the saved list every time it opens: servers in saved order
// (capped), then Show and Exit. The native menu glue renders exactly these entries.
public partial class MainViewModelTests
{
    private static MainViewModel TrayViewModel(params (string Name, string Address)[] servers)
    {
        var repository = new InMemoryServerRepository();

        foreach (var (name, address) in servers)
        {
            repository.Add(SavedServer.Create(name, address));
        }

        return CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            enrichment: new FakeServerEnrichmentService());
    }

    private static IEnumerable<string> ServerLabels(IReadOnlyList<TrayMenuEntry> menu) =>
        menu.Where(e => e.Kind == TrayMenuEntryKind.Server).Select(e => e.Label);

    [Fact]
    public void BuildTrayMenu_ShouldListSavedServersInOrderThenShowAndExit()
    {
        // Given
        var vm = TrayViewModel(("Alpha", "cfx.re/join/aaa111"), ("Bravo", "149.56.120.52:30120"));

        // When
        var menu = vm.BuildTrayMenu();

        // Then
        Assert.Equal(
            [
                new TrayMenuEntry(TrayMenuEntryKind.Server, "Alpha", "cfx.re/join/aaa111", true),
                new TrayMenuEntry(TrayMenuEntryKind.Server, "Bravo", "149.56.120.52:30120", true),
                new TrayMenuEntry(TrayMenuEntryKind.Separator, string.Empty, null, true),
                new TrayMenuEntry(TrayMenuEntryKind.Show, "Show", null, true),
                new TrayMenuEntry(TrayMenuEntryKind.Exit, "Exit", null, true)
            ],
            menu);
    }

    [Fact]
    public void BuildTrayMenu_WithNoSavedServers_ShouldShowOneDisabledPlaceholder()
    {
        // Given
        var vm = TrayViewModel();

        // When
        var menu = vm.BuildTrayMenu();

        // Then
        Assert.Equal(
            [
                new TrayMenuEntry(TrayMenuEntryKind.NoServers, "No saved servers", null, false),
                new TrayMenuEntry(TrayMenuEntryKind.Separator, string.Empty, null, true),
                new TrayMenuEntry(TrayMenuEntryKind.Show, "Show", null, true),
                new TrayMenuEntry(TrayMenuEntryKind.Exit, "Exit", null, true)
            ],
            menu);
    }

    [Fact]
    public void BuildTrayMenu_PastTheCap_ShouldListTheFirstServersThenAllServers()
    {
        // Given — one more server than the menu holds.
        var servers = Enumerable.Range(1, TrayMenu.MaxServers + 1)
            .Select(i => ($"Server {i}", $"10.0.0.{i}:30120"))
            .ToArray();
        var vm = TrayViewModel(servers);

        // When
        var menu = vm.BuildTrayMenu();

        // Then
        Assert.Equal(
            servers.Take(TrayMenu.MaxServers).Select(s => s.Item1),
            ServerLabels(menu));
        Assert.Equal(
            new TrayMenuEntry(TrayMenuEntryKind.ShowAll, "All servers...", null, true),
            menu[TrayMenu.MaxServers]);
        Assert.Equal(TrayMenuEntryKind.Separator, menu[TrayMenu.MaxServers + 1].Kind);
    }

    [Fact]
    public void BuildTrayMenu_ExactlyAtTheCap_ShouldNotOfferAllServers()
    {
        // Given
        var servers = Enumerable.Range(1, TrayMenu.MaxServers)
            .Select(i => ($"Server {i}", $"10.0.0.{i}:30120"))
            .ToArray();
        var vm = TrayViewModel(servers);

        // When
        var menu = vm.BuildTrayMenu();

        // Then
        Assert.Equal(TrayMenu.MaxServers, ServerLabels(menu).Count());
        Assert.DoesNotContain(menu, e => e.Kind == TrayMenuEntryKind.ShowAll);
    }

    [Fact]
    public void BuildTrayMenu_WithAVeryLongName_ShouldCutItWithAnEllipsis()
    {
        // Given
        var longName = new string('x', TrayMenu.MaxLabelLength + 20);
        var vm = TrayViewModel((longName, "cfx.re/join/aaa111"));

        // When
        var label = Assert.Single(ServerLabels(vm.BuildTrayMenu()));

        // Then
        Assert.Equal(TrayMenu.MaxLabelLength, label.Length);
        Assert.EndsWith("…", label);
        Assert.StartsWith(new string('x', TrayMenu.MaxLabelLength - 1), label);
    }

    [Fact]
    public void BuildTrayMenu_WhenTheCutFallsInsideAnEmoji_ShouldNotSplitIt()
    {
        // Given — the emoji's high surrogate sits exactly at the last kept position.
        var longName = new string('x', TrayMenu.MaxLabelLength - 2) + "🎮" + new string('y', 10);
        var vm = TrayViewModel((longName, "cfx.re/join/aaa111"));

        // When
        var label = Assert.Single(ServerLabels(vm.BuildTrayMenu()));

        // Then — no lone surrogate before the ellipsis.
        Assert.False(char.IsHighSurrogate(label[^2]));
        Assert.Equal(new string('x', TrayMenu.MaxLabelLength - 2) + "…", label);
    }

    [Fact]
    public void BuildTrayMenu_ShouldIgnoreTheSearchFilter()
    {
        // Given — the search box filters the visible list, never the tray.
        var vm = TrayViewModel(("Alpha", "cfx.re/join/aaa111"), ("Bravo", "cfx.re/join/bbb222"));
        vm.ServerSearchText = "Bravo";

        // When / Then
        Assert.Equal(["Alpha", "Bravo"], ServerLabels(vm.BuildTrayMenu()));
    }

    [Fact]
    public void BuildTrayMenu_ShouldFollowAddRemoveReorderAndRename()
    {
        // Given
        var vm = TrayViewModel(("Alpha", "cfx.re/join/aaa111"), ("Bravo", "cfx.re/join/bbb222"));

        // When — add
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Charlie";
        vm.DialogServerAddress = "cfx.re/join/ccc333";
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal(["Alpha", "Bravo", "Charlie"], ServerLabels(vm.BuildTrayMenu()));

        // When — reorder
        vm.MoveServerToIndex(vm.SavedServers[2], 0);

        // Then
        Assert.Equal(["Charlie", "Alpha", "Bravo"], ServerLabels(vm.BuildTrayMenu()));

        // When — rename
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[1]);
        vm.DialogServerName = "Alpha Renamed";
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal(["Charlie", "Alpha Renamed", "Bravo"], ServerLabels(vm.BuildTrayMenu()));

        // When — remove
        vm.DeleteServerCommand.Execute(vm.SavedServers[2]);
        vm.ConfirmCommand.Execute(null);

        // Then
        Assert.Equal(["Charlie", "Alpha Renamed"], ServerLabels(vm.BuildTrayMenu()));
    }

    [Fact]
    public async Task BuildTrayMenu_WhileBusy_ShouldDisableServersButKeepShowAndExit()
    {
        // Given — a connect in flight.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Alpha", "cfx.re/join/aaa111"));
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(),
            CfxJson("gta5"),
            repository: repository,
            cfxHttpClient: new HttpClient(new GatedHttpHandler(gate.Task, CfxJson("gta5"))));
        vm.ServerAddress = "cfx.re/join/first";
        var inFlight = vm.ConnectAsync();

        // When
        var menu = vm.BuildTrayMenu();

        // Then
        Assert.False(menu.Single(e => e.Kind == TrayMenuEntryKind.Server).IsEnabled);
        Assert.True(menu.Single(e => e.Kind == TrayMenuEntryKind.Show).IsEnabled);
        Assert.True(menu.Single(e => e.Kind == TrayMenuEntryKind.Exit).IsEnabled);

        gate.SetResult();
        await inFlight;

        // Then — idle again, the next menu enables them.
        Assert.True(vm.BuildTrayMenu().Single(e => e.Kind == TrayMenuEntryKind.Server).IsEnabled);
    }

    [Fact]
    public void BuildTrayMenu_ShouldUseTheCurrentLanguage()
    {
        // Given
        var localizer = Localizer.FromEmbeddedResources(() => "en-US");
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), localizer: localizer);

        // When
        localizer.SetLanguage("es");
        var menu = vm.BuildTrayMenu();

        // Then
        Assert.Equal("No hay servidores guardados", menu[0].Label);
        Assert.Equal("Mostrar", menu.Single(e => e.Kind == TrayMenuEntryKind.Show).Label);
        Assert.Equal("Salir", menu.Single(e => e.Kind == TrayMenuEntryKind.Exit).Label);
    }
}
