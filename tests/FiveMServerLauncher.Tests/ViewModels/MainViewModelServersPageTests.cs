using System.ComponentModel;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;
using FiveMServerLauncher.Tests.Launch;
using FiveMServerLauncher.Tests.Service;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

// The Servers page rules (ui-redesign ticket 06): the All / Online / Offline filter with
// live counts, the reorder gate, the always-selected rule and Copy address.
public partial class MainViewModelTests
{
    // Alpha and Delta are live in the catalog; Bravo has a cfx id but is not listed now
    // (offline); Charlie has no cfx id at all (not listed).
    private static (MainViewModel Vm, InMemoryServerRepository Repository, FakeServerEnrichmentService Enrichment)
        ServersPageViewModel(FakeClipboardService? clipboard = null)
    {
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Alpha", "cfx.re/join/aaaaaa"));
        repository.Add(SavedServer.Create("Bravo", "cfx.re/join/bbbbbb"));
        repository.Add(SavedServer.Create("Charlie", "10.0.0.5:30120"));
        repository.Add(SavedServer.Create("Delta", "cfx.re/join/dddddd"));
        var enrichment = new FakeServerEnrichmentService
        {
            Presence = new Dictionary<string, ServerPresence>
            {
                ["aaaaaa"] = new(true, 10, 64, GameClient.FiveM),
                ["dddddd"] = new(true, 3, 32, GameClient.RedM)
            }
        };
        var vm = CreateViewModel(
            new FakeGameProcessLauncher(), CfxJson("gta5"), repository, enrichment: enrichment, clipboard: clipboard);
        return (vm, repository, enrichment);
    }

    private static IReadOnlyList<string> VisibleNames(MainViewModel vm) =>
        vm.SavedServersView.Cast<SavedServerItem>().Select(s => s.Name).ToList();

    [Fact]
    public async Task ServerFilterCounts_ShouldCountOfflineAsEveryRowThatIsNotOnline()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();

        // When
        await vm.RefreshServerInfoAsync();

        // Then — Bravo (offline) and Charlie (not listed) are both "Offline"
        Assert.Equal(4, vm.AllServerCount);
        Assert.Equal(2, vm.OnlineServerCount);
        Assert.Equal(2, vm.OfflineServerCount);
    }

    [Fact]
    public void ServerFilter_ShouldStartOnAll()
    {
        // Given / When
        var (vm, _, _) = ServersPageViewModel();

        // Then
        Assert.Equal(SavedServerFilter.All, vm.ServerFilter);
        Assert.Equal(new[] { "Alpha", "Bravo", "Charlie", "Delta" }, VisibleNames(vm));
    }

    [Theory]
    [InlineData(SavedServerFilter.All, new[] { "Alpha", "Bravo", "Charlie", "Delta" })]
    [InlineData(SavedServerFilter.Online, new[] { "Alpha", "Delta" })]
    [InlineData(SavedServerFilter.Offline, new[] { "Bravo", "Charlie" })]
    public async Task ServerFilter_ShouldShowOnlyTheMatchingRows(SavedServerFilter filter, string[] expected)
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        await vm.RefreshServerInfoAsync();

        // When
        vm.ServerFilter = filter;

        // Then
        Assert.Equal(expected, VisibleNames(vm));
    }

    [Fact]
    public async Task ServerFilter_ShouldComposeWithTheSearchText()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        await vm.RefreshServerInfoAsync();

        // When — "a" matches every name; the chip narrows it
        vm.ServerFilter = SavedServerFilter.Offline;
        vm.ServerSearchText = "char";

        // Then
        Assert.Equal(new[] { "Charlie" }, VisibleNames(vm));
    }

    [Fact]
    public async Task ServerFilterCounts_WhenAServerGoesOffline_ShouldFollowAndRefreshTheView()
    {
        // Given — on the Online chip
        var (vm, _, enrichment) = ServersPageViewModel();
        await vm.RefreshServerInfoAsync();
        vm.ServerFilter = SavedServerFilter.Online;
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When — Alpha leaves the catalog
        enrichment.Presence = new Dictionary<string, ServerPresence>
        {
            ["dddddd"] = new(true, 3, 32, GameClient.RedM)
        };
        await vm.RefreshServerInfoAsync();

        // Then
        Assert.Equal(1, vm.OnlineServerCount);
        Assert.Equal(3, vm.OfflineServerCount);
        Assert.Contains(nameof(MainViewModel.OnlineServerCount), raised);
        Assert.Contains(nameof(MainViewModel.OfflineServerCount), raised);
        Assert.Equal(new[] { "Delta" }, VisibleNames(vm));
    }

    [Fact]
    public void ServerFilterCounts_ShouldFollowAddAndDelete()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();

        // When — a new (not listed) server is saved (the save refreshes presence, so
        // Alpha and Delta come online), then Alpha is deleted
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Echo";
        vm.DialogServerAddress = "cfx.re/join/eeeeee";
        vm.SaveServerDialogCommand.Execute(null);
        vm.DeleteServerCommand.Execute(vm.SavedServers[0]);
        vm.ConfirmCommand.Execute(null);

        // Then
        Assert.Equal(4, vm.AllServerCount);
        Assert.Equal(1, vm.OnlineServerCount);
        Assert.Equal(3, vm.OfflineServerCount);
    }

    [Theory]
    [InlineData(SavedServerFilter.Online)]
    [InlineData(SavedServerFilter.Offline)]
    public void IsReorderAvailable_WhenTheFilterIsNotAll_ShouldBeFalse(SavedServerFilter filter)
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.ServerFilter = filter;

        // Then — a filtered view is a partial list: no reorder, no move flags
        Assert.False(vm.IsReorderAvailable);
        Assert.Contains(nameof(MainViewModel.IsReorderAvailable), raised);
        Assert.All(vm.SavedServers, row => Assert.False(row.CanMoveUp || row.CanMoveDown));
        Assert.False(vm.MoveServerDownCommand.CanExecute(vm.SavedServers[0]));

        // When the chip returns to All
        vm.ServerFilter = SavedServerFilter.All;

        // Then
        Assert.True(vm.IsReorderAvailable);
        Assert.True(vm.SavedServers[0].CanMoveDown);
    }

    [Fact]
    public void MoveServerToIndex_WhenTheFilterIsNotAll_ShouldLeaveListAndStoreUntouched()
    {
        // Given
        var (vm, repository, _) = ServersPageViewModel();
        vm.ServerFilter = SavedServerFilter.Offline;

        // When
        vm.MoveServerToIndex(vm.SavedServers[0], 2);

        // Then
        Assert.Equal("Alpha", vm.SavedServers[0].Name);
        Assert.Equal("Alpha", repository.GetAll()[0].Name);
    }

    [Fact]
    public void Ctor_WithSavedServers_ShouldSelectTheFirstRowAndFillTheAddress()
    {
        // Given / When
        var (vm, _, _) = ServersPageViewModel();

        // Then
        Assert.Same(vm.SavedServers[0], vm.SelectedServer);
        Assert.True(vm.SavedServers[0].IsSelected);
        Assert.Equal("cfx.re/join/aaaaaa", vm.ServerAddress);
    }

    [Fact]
    public void Ctor_WithNoSavedServers_ShouldSelectNothingAndShowTheEmptyState()
    {
        // Given / When
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"));

        // Then
        Assert.Null(vm.SelectedServer);
        Assert.True(vm.HasNoSavedServers);
        Assert.False(vm.HasNoServerMatch);
        Assert.True(string.IsNullOrEmpty(vm.ServerAddress));
    }

    [Theory]
    [InlineData(1, "Charlie")] // a middle row: its next neighbour
    [InlineData(3, "Charlie")] // the last row: its previous neighbour
    [InlineData(0, "Bravo")]
    public void DeleteServer_WhenConfirmed_ShouldSelectTheNeighbour(int index, string expected)
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        vm.SelectedServer = vm.SavedServers[index];

        // When
        vm.DeleteServerCommand.Execute(vm.SavedServers[index]);
        vm.ConfirmCommand.Execute(null);

        // Then
        Assert.Equal(expected, vm.SelectedServer?.Name);
        Assert.Equal(vm.SelectedServer!.Address, vm.ServerAddress);
    }

    [Fact]
    public void DeleteServer_OfAnUnselectedRow_ShouldKeepTheSelection()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        vm.SelectedServer = vm.SavedServers[2];

        // When
        vm.DeleteServerCommand.Execute(vm.SavedServers[0]);
        vm.ConfirmCommand.Execute(null);

        // Then
        Assert.Equal("Charlie", vm.SelectedServer?.Name);
    }

    [Fact]
    public void DeleteServer_OfTheLastServer_ShouldSelectNothingAndShowTheEmptyState()
    {
        // Given
        var repository = new InMemoryServerRepository();
        repository.Add(SavedServer.Create("Only", "cfx.re/join/oooooo"));
        var vm = CreateViewModel(new FakeGameProcessLauncher(), CfxJson("gta5"), repository);
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.DeleteServerCommand.Execute(vm.SavedServers[0]);
        vm.ConfirmCommand.Execute(null);

        // Then
        Assert.Null(vm.SelectedServer);
        Assert.True(vm.HasNoSavedServers);
        Assert.Contains(nameof(MainViewModel.HasNoSavedServers), raised);
    }

    [Fact]
    public async Task ServerFilter_WhenItHidesTheSelection_ShouldSelectTheFirstVisibleRow()
    {
        // Given — Alpha (online) is selected
        var (vm, _, _) = ServersPageViewModel();
        await vm.RefreshServerInfoAsync();

        // When
        vm.ServerFilter = SavedServerFilter.Offline;

        // Then
        Assert.Equal("Bravo", vm.SelectedServer?.Name);
        Assert.Equal("cfx.re/join/bbbbbb", vm.ServerAddress);
    }

    [Fact]
    public void ServerSearchText_WhenItKeepsTheSelectionVisible_ShouldNotChangeIt()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        vm.SelectedServer = vm.SavedServers[3];

        // When — "a" is in every name
        vm.ServerSearchText = "a";

        // Then
        Assert.Equal("Delta", vm.SelectedServer?.Name);
    }

    [Fact]
    public void ServerSearchText_WithNoMatch_ShouldSelectNothingAndShowTheNoMatchState()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.ServerSearchText = "zzz";

        // Then
        Assert.Null(vm.SelectedServer);
        Assert.True(vm.HasNoServerMatch);
        Assert.False(vm.HasNoSavedServers);
        Assert.Contains(nameof(MainViewModel.HasNoServerMatch), raised);

        // When the search clears, the first row is selected again
        vm.ServerSearchText = "";

        // Then
        Assert.False(vm.HasNoServerMatch);
        Assert.Equal("Alpha", vm.SelectedServer?.Name);
    }

    [Fact]
    public void SelectedServer_SetToNullWhileItIsVisible_ShouldKeepTheSelection()
    {
        // Given — a list deselect (Ctrl+click) must not leave the page without a server
        var (vm, _, _) = ServersPageViewModel();
        vm.SelectedServer = vm.SavedServers[1];
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        vm.SelectedServer = null;

        // Then — kept, and re-announced so the list re-selects it
        Assert.Equal("Bravo", vm.SelectedServer?.Name);
        Assert.Contains(nameof(MainViewModel.SelectedServer), raised);
    }

    [Fact]
    public void SaveServerDialog_WhenEditingTheSelectedServer_ShouldSelectTheEditedRow()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        vm.SelectedServer = vm.SavedServers[2];
        vm.OpenEditServerDialogCommand.Execute(vm.SavedServers[2]);
        vm.DialogServerName = "Charlie Renamed";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Same(vm.SavedServers[2], vm.SelectedServer);
        Assert.Equal("Charlie Renamed", vm.SelectedServer!.Name);
        Assert.True(vm.SelectedServer.IsSelected);
    }

    [Fact]
    public void SaveServerDialog_WhenAdding_ShouldSelectTheNewRow()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        vm.OpenAddServerDialogCommand.Execute(null);
        vm.DialogServerName = "Echo";
        vm.DialogServerAddress = "cfx.re/join/eeeeee";

        // When
        vm.SaveServerDialogCommand.Execute(null);

        // Then
        Assert.Equal("Echo", vm.SelectedServer?.Name);
        Assert.Equal("cfx.re/join/eeeeee", vm.ServerAddress);
    }

    [Fact]
    public void SaveBrowserServer_ShouldKeepTheCurrentSelection()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        vm.SelectedServer = vm.SavedServers[1];

        // When
        vm.SaveBrowserServer(ServerBrowserItem.FromServer(new Master.Server
        {
            EndPoint = "ffffff",
            Data = new Master.ServerData { Vars = { ["sv_projectName"] = "Foxtrot" } }
        }));

        // Then
        Assert.Equal("Bravo", vm.SelectedServer?.Name);
        Assert.Equal(5, vm.AllServerCount);
    }

    [Fact]
    public void CopyAddressCommand_ShouldPutTheRowAddressOnTheClipboard()
    {
        // Given
        var clipboard = new FakeClipboardService();
        var (vm, _, _) = ServersPageViewModel(clipboard);

        // When
        vm.CopyAddressCommand.Execute(vm.SavedServers[2]);

        // Then
        Assert.Equal(new[] { "10.0.0.5:30120" }, clipboard.Texts);
        Assert.Equal("Address copied", vm.StatusText);
    }

    [Fact]
    public void CopyAddressCommand_WithoutParameter_ShouldCopyTheSelectedServer()
    {
        // Given
        var clipboard = new FakeClipboardService();
        var (vm, _, _) = ServersPageViewModel(clipboard);
        vm.SelectedServer = vm.SavedServers[3];

        // When
        vm.CopyAddressCommand.Execute(null);

        // Then
        Assert.Equal(new[] { "cfx.re/join/dddddd" }, clipboard.Texts);
    }

    [Fact]
    public void CopyAddressCommand_WhenTheClipboardRefuses_ShouldReportIt()
    {
        // Given — another app holds the clipboard open
        var clipboard = new FakeClipboardService { Accepts = false };
        var (vm, _, _) = ServersPageViewModel(clipboard);

        // When
        vm.CopyAddressCommand.Execute(vm.SavedServers[0]);

        // Then
        Assert.Equal("Could not copy address", vm.StatusText);
    }

    [Fact]
    public void CopyAddressCommand_WhenTheClipboardIsNotWired_ShouldDoNothing()
    {
        // Given
        var (vm, _, _) = ServersPageViewModel();
        var status = vm.StatusText;

        // When
        vm.CopyAddressCommand.Execute(vm.SavedServers[0]);

        // Then
        Assert.Equal(status, vm.StatusText);
    }
}
