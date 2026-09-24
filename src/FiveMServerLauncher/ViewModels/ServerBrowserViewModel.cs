using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.ViewModels;

public sealed class ServerBrowserViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan DefaultRefreshCooldown = TimeSpan.FromSeconds(15);

    private readonly ServerCatalog _catalog;
    private readonly IServerEnrichmentService _enrichment;
    private readonly IServerRepository _repository;
    private readonly TimeSpan _refreshCooldown;

    public ServerBrowserViewModel(
        ServerCatalog catalog,
        IServerEnrichmentService enrichment,
        IServerRepository repository,
        TimeSpan? refreshCooldown = null)
    {
        _catalog = catalog;
        _enrichment = enrichment;
        _repository = repository;
        _refreshCooldown = refreshCooldown ?? DefaultRefreshCooldown;
        Servers = new ObservableCollection<ServerBrowserItem>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsRefreshing);
        _selectedGameFilterOption = GameFilterOptions[0];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private ObservableCollection<ServerBrowserItem> _servers = [];

    public ObservableCollection<ServerBrowserItem> Servers
    {
        get => _servers;
        private set
        {
            if (!SetProperty(ref _servers, value))
            {
                return;
            }

            ServersView = CollectionViewSource.GetDefaultView(value);
            ServersView.Filter = MatchesFilters;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServersView)));
        }
    }

    public ICollectionView ServersView { get; private set; } = CollectionViewSource.GetDefaultView(new List<ServerBrowserItem>());

    public ICommand RefreshCommand { get; }

    public sealed record GameFilterOption(string Label, GameClient? Game);

    public IReadOnlyList<GameFilterOption> GameFilterOptions { get; } =
    [
        new GameFilterOption("ALL", null),
        new GameFilterOption("FIVEM", GameClient.FiveM),
        new GameFilterOption("ENHANCED", GameClient.FiveMEnhanced),
        new GameFilterOption("REDM", GameClient.RedM)
    ];

    private GameFilterOption _selectedGameFilterOption = null!;

    public GameFilterOption SelectedGameFilterOption
    {
        get => _selectedGameFilterOption;
        set
        {
            if (SetProperty(ref _selectedGameFilterOption, value))
            {
                GameFilter = value.Game;
            }
        }
    }

    private bool _isRefreshing;

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set => SetProperty(ref _isRefreshing, value);
    }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;

        try
        {
            await _enrichment.RefreshAsync(forceRefresh: true);
        }
        catch (Exception)
        {
            // A failed manual refresh keeps the current rows.
        }

        await LoadAsync();
        await Task.Delay(_refreshCooldown);
        IsRefreshing = false;
        CommandManager.InvalidateRequerySuggested();
    }

    private GameClient? _gameFilter;

    public GameClient? GameFilter
    {
        get => _gameFilter;
        set => SetFilter(ref _gameFilter, value);
    }

    private bool _hideFull;

    public bool HideFull
    {
        get => _hideFull;
        set => SetFilter(ref _hideFull, value);
    }

    private bool _hideEmpty;

    public bool HideEmpty
    {
        get => _hideEmpty;
        set => SetFilter(ref _hideEmpty, value);
    }

    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set => SetFilter(ref _searchText, value);
    }

    private void SetFilter<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (SetProperty(ref field, value, name))
        {
            ServersView.Refresh();
        }
    }

    private bool _loadFailed;

    public bool LoadFailed
    {
        get => _loadFailed;
        private set => SetProperty(ref _loadFailed, value);
    }

    public async Task LoadAsync()
    {
        var snapshot = await _catalog.GetSnapshotAsync();

        if (snapshot is null)
        {
            LoadFailed = true;
            return;
        }

        LoadFailed = false;

        // Build the 33k rows off the UI thread and swap the collection wholesale —
        // 33k individual ObservableCollection.Adds would freeze the UI on open.
        var items = await Task.Run(() => snapshot
            .Where(s => s.Data is not null)
            .Select(ServerBrowserItem.FromServer)
            .ToList());

        foreach (var item in items)
        {
            item.SetIconLoader(() => LoadIconAsync(item));
        }

        Servers = new ObservableCollection<ServerBrowserItem>(items);
        MarkSavedRows();
    }

    private async Task LoadIconAsync(ServerBrowserItem item)
    {
        var icon = item.IconVersion is not null
            ? await _enrichment.GetIconAsync(item.CfxId, item.IconVersion)
            : await _enrichment.GetIconAsync(item.CfxId);

        if (icon is not null)
        {
            item.SetIcon(icon);
        }
    }

    private void MarkSavedRows()
    {
        var saved = _repository.GetAll();

        foreach (var row in Servers)
        {
            row.SetSaved(saved.Any(s =>
                s.MatchesAddress(row.Address)
                || (s.CfxId is not null && s.CfxId == row.CfxId)));
        }
    }

    private bool MatchesFilters(object item)
    {
        var server = (ServerBrowserItem)item;

        if (_gameFilter is not null && server.Game != _gameFilter)
        {
            return false;
        }

        if (_hideFull && server.MaxPlayers > 0 && server.Players >= server.MaxPlayers)
        {
            return false;
        }

        if (_hideEmpty && server.Players == 0)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(_searchText)
               || server.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
