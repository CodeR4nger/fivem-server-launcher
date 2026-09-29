using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.ViewModels;

public sealed class ServerBrowserViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan DefaultRefreshCooldown = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DefaultSearchDebounce = TimeSpan.FromMilliseconds(250);

    private readonly ServerCatalog _catalog;
    private readonly IServerEnrichmentService _enrichment;
    private readonly IServerRepository _repository;
    private readonly TimeSpan _refreshCooldown;

    private readonly ILocalizer _localizer;
    private readonly TimeSpan _searchDebounce;
    private readonly Func<TimeSpan, CancellationToken, Task> _searchDelay;

    public ServerBrowserViewModel(
        ServerCatalog catalog,
        IServerEnrichmentService enrichment,
        IServerRepository repository,
        TimeSpan? refreshCooldown = null,
        ILocalizer? localizer = null,
        TimeSpan? searchDebounce = null,
        Func<TimeSpan, CancellationToken, Task>? searchDelay = null)
    {
        _catalog = catalog;
        _enrichment = enrichment;
        _repository = repository;
        _localizer = localizer ?? DefaultLocalizer.Get();
        _localizer.LanguageChanged += OnLanguageChanged;
        _refreshCooldown = refreshCooldown ?? DefaultRefreshCooldown;
        _searchDebounce = searchDebounce ?? DefaultSearchDebounce;
        _searchDelay = searchDelay ?? ((duration, token) => Task.Delay(duration, token));
        Servers = new ObservableCollection<ServerBrowserItem>();
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsRefreshing);
        GameFilterOptions = BuildGameFilterOptions();
        _selectedGameFilterOption = GameFilterOptions[0];
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedGame = _selectedGameFilterOption.Game;
        GameFilterOptions = BuildGameFilterOptions();
        _selectedGameFilterOption = GameFilterOptions.Single(o => o.Game == selectedGame);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameFilterOptions)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedGameFilterOption)));
    }

    public sealed record GameFilterOption(string Label, GameClient? Game);

    public IReadOnlyList<GameFilterOption> GameFilterOptions { get; private set; }

    private IReadOnlyList<GameFilterOption> BuildGameFilterOptions()
    {
        return
        [
            new GameFilterOption(_localizer.Get("GameFilterAll"), null),
            new GameFilterOption(_localizer.Get("GameFilterFiveM"), GameClient.FiveM),
            new GameFilterOption(_localizer.Get("GameFilterEnhanced"), GameClient.FiveMEnhanced),
            new GameFilterOption(_localizer.Get("GameFilterRedM"), GameClient.RedM)
        ];
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
        set
        {
            if (SetProperty(ref _searchText, value, nameof(SearchText)))
            {
                ScheduleSearchRefresh();
            }
        }
    }

    private CancellationTokenSource? _searchRefreshDebounce;

    // Typing must not re-run the filter over the 30k-row view per keystroke: the
    // refresh waits out an idle window and rapid keystrokes coalesce into the final
    // text. Discrete filters (game/hide) still refresh immediately.
    private void ScheduleSearchRefresh()
    {
        _searchRefreshDebounce?.Cancel();

        var cancellation = _searchRefreshDebounce = new CancellationTokenSource();
        _ = DebounceSearchRefreshAsync(cancellation);
    }

    private async Task DebounceSearchRefreshAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await _searchDelay(_searchDebounce, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellation.IsCancellationRequested)
        {
            return;
        }

        try
        {
            ServersView.Refresh();
        }
        catch (Exception)
        {
            // A throwing refresh must never crash the fire-and-forget dispatcher
            // continuation; the next keystroke starts a fresh debounce.
        }
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
        // The snapshot is the proof: a server publishing no iconVersion has no icon,
        // so the row never probes /single/ for one (request hygiene).
        if (item.IconVersion is null)
        {
            return;
        }

        var icon = await _enrichment.GetIconAsync(item.CfxId, item.IconVersion);

        if (icon is not null)
        {
            item.SetIcon(icon);
        }
    }

    private void MarkSavedRows()
    {
        var saved = _repository.GetAll();

        // Keyed lookup: the per-row scan over every saved server was O(rows x saved)
        // on a 30k-row load.
        var savedAddresses = new HashSet<string>(
            saved.Select(s => s.Address),
            StringComparer.OrdinalIgnoreCase);
        var savedIds = new HashSet<string>(
            saved.Where(s => s.CfxId is not null).Select(s => s.CfxId!));

        foreach (var row in Servers)
        {
            row.SetSaved(savedAddresses.Contains(row.Address)
                || (row.CfxId is not null && savedIds.Contains(row.CfxId)));
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
