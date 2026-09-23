using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private static readonly GameClient[] OpenCandidates = [GameClient.FiveM, GameClient.FiveMEnhanced, GameClient.RedM];

    private readonly ServerResolver _resolver;
    private readonly GameLauncher _launcher;
    private readonly IServerRepository _serverRepository;
    private readonly ExternalAppPreparer _preparer;
    private readonly IClientInstallLocator _installLocator;
    private readonly ConfigurationRepository _settingsRepository;
    private readonly IServerEnrichmentService _enrichment;
    private readonly ICfxStatusService _cfxStatus;
    private readonly object _captureGate = new();
    private readonly List<Task> _pendingCaptures = [];

    private LauncherSettings _settings;

    private string _serverAddress = string.Empty;
    private string _statusText = "Ready";
    private bool _isBusy;
    private bool _isSettingsOpen;
    private SavedServerItem? _selectedServer;
    private InstalledClientOption? _selectedOpenClient;
    private InstalledClientOption? _preferredClientOption;

    public MainViewModel(
        ServerResolver resolver,
        GameLauncher launcher,
        IServerRepository serverRepository,
        ExternalAppPreparer preparer,
        IClientInstallLocator installLocator,
        ConfigurationRepository settingsRepository,
        IServerEnrichmentService enrichment,
        ICfxStatusService cfxStatus,
        TimeSpan? refreshCooldown = null)
    {
        _resolver = resolver;
        _launcher = launcher;
        _serverRepository = serverRepository;
        _preparer = preparer;
        _installLocator = installLocator;
        _settingsRepository = settingsRepository;
        _enrichment = enrichment;
        _cfxStatus = cfxStatus;
        _refreshCooldown = refreshCooldown ?? DefaultRefreshCooldown;
        _settings = settingsRepository.Load();
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        DeleteServerCommand = new RelayCommand(DeleteServer, CanDeleteServer);
        OpenClientCommand = new AsyncRelayCommand(OpenClientAsync, CanOpenClient);
        SelectOpenClientCommand = new RelayCommand(SelectOpenClient);
        SettingsCommand = new RelayCommand(ToggleSettings);
        ToggleDevModeCommand = new RelayCommand(ToggleDevMode);
        OpenAddServerDialogCommand = new RelayCommand(OpenAddServerDialog);
        OpenEditServerDialogCommand = new RelayCommand(OpenEditServerDialog);
        SaveServerDialogCommand = new AsyncRelayCommand(SaveServerDialogAsync);
        CancelServerDialogCommand = new RelayCommand(CloseServerDialog);
        RefreshServersCommand = new AsyncRelayCommand(RefreshServersAsync, () => !IsRefreshingServers);
        ToggleDevClientCommand = new RelayCommand(() =>
            DevClient = DevClient == GameClient.FiveM ? GameClient.RedM : GameClient.FiveM);
        DevLaunchCommand = new AsyncRelayCommand((object? secondClient) => DevLaunchAsync(secondClient is true), () => !IsBusy);

        SavedServers = new ObservableCollection<SavedServerItem>(
            serverRepository.GetAll().Select(ToItem));
        SavedServersView = CollectionViewSource.GetDefaultView(SavedServers);
        SavedServersView.Filter = MatchesServerSearch;
        AvailableOpenClients = new ObservableCollection<InstalledClientOption>();

        FiveMStatus = new CfxStatusItem(GameClient.FiveM);
        FiveMEnhancedStatus = new CfxStatusItem(GameClient.FiveMEnhanced);
        RedMStatus = new CfxStatusItem(GameClient.RedM);
        CfxStatuses = [FiveMStatus, FiveMEnhancedStatus, RedMStatus];
    }

    public ICommand ConnectCommand { get; }

    public ICommand DeleteServerCommand { get; }

    public ICommand OpenClientCommand { get; }

    public ICommand SelectOpenClientCommand { get; }

    public ICommand SettingsCommand { get; }

    public ICommand OpenAddServerDialogCommand { get; }

    public ICommand OpenEditServerDialogCommand { get; }

    public ICommand SaveServerDialogCommand { get; }

    public ICommand CancelServerDialogCommand { get; }

    public ICommand RefreshServersCommand { get; }

    private bool _isRefreshingServers;

    private static readonly TimeSpan DefaultRefreshCooldown = TimeSpan.FromSeconds(15);
    private readonly TimeSpan _refreshCooldown;

    public bool IsRefreshingServers
    {
        get => _isRefreshingServers;
        private set => SetProperty(ref _isRefreshingServers, value);
    }

    private async Task RefreshServersAsync()
    {
        IsRefreshingServers = true;

        try
        {
            await RefreshServerInfoAsync(forceRefresh: true);
        }
        catch (Exception)
        {
            // A failed manual refresh leaves rows untouched.
        }

        // Keep the command disabled for the shared cooldown so the expensive
        // catalog download can't be spammed (scrolling must not re-enable it).
        await Task.Delay(_refreshCooldown);
        IsRefreshingServers = false;
        CommandManager.InvalidateRequerySuggested();
    }

    private SavedServerItem? _editingServer;
    private bool _isServerDialogOpen;
    private string _dialogServerName = string.Empty;
    private string _dialogServerAddress = string.Empty;
    private bool _dialogRequiresSteam;
    private bool _dialogRequiresDiscord;
    private string _dialogError = string.Empty;

    public SavedServerItem? EditingServer
    {
        get => _editingServer;
        private set
        {
            if (SetProperty(ref _editingServer, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerDialogTitle)));
            }
        }
    }

    public bool IsServerDialogOpen
    {
        get => _isServerDialogOpen;
        private set => SetProperty(ref _isServerDialogOpen, value);
    }

    public string ServerDialogTitle => EditingServer is null ? "NEW SERVER" : "EDIT SERVER";

    public string DialogError
    {
        get => _dialogError;
        private set => SetProperty(ref _dialogError, value);
    }

    public string DialogServerName
    {
        get => _dialogServerName;
        set => SetProperty(ref _dialogServerName, value);
    }

    public string DialogServerAddress
    {
        get => _dialogServerAddress;
        set => SetProperty(ref _dialogServerAddress, value);
    }

    public bool DialogRequiresSteam
    {
        get => _dialogRequiresSteam;
        set => SetProperty(ref _dialogRequiresSteam, value);
    }

    public bool DialogRequiresDiscord
    {
        get => _dialogRequiresDiscord;
        set => SetProperty(ref _dialogRequiresDiscord, value);
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    private bool _isDevMode;
    private GameClient _devClient = GameClient.FiveM;

    public bool IsDevMode
    {
        get => _isDevMode;
        private set => SetProperty(ref _isDevMode, value);
    }

    public ICommand ToggleDevModeCommand { get; }

    public GameClient DevClient
    {
        get => _devClient;
        private set
        {
            if (SetProperty(ref _devClient, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevClientLabel)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevClientButtonText)));
            }
        }
    }

    public string DevClientLabel => InstalledClientOption.DisplayNameOf(DevClient);

    public string DevClientButtonText => $"CLIENT: {DevClientLabel}";

    public ICommand ToggleDevClientCommand { get; }

    public string DevGameBuild
    {
        get => _settings.DevGameBuild?.ToString() ?? string.Empty;
        set
        {
            var trimmed = value.Trim();
            var build = int.TryParse(trimmed, out var parsed) ? parsed : (int?)null;

            if (_settings.DevGameBuild == build)
            {
                return;
            }

            _settings.DevGameBuild = build;
            SaveSettings();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevGameBuild)));
        }
    }

    public int? DevPureMode
    {
        get => _settings.DevPureMode;
        set
        {
            if (_settings.DevPureMode == value)
            {
                return;
            }

            _settings.DevPureMode = value;
            SaveSettings();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevPureMode)));
        }
    }

    public ICommand DevLaunchCommand { get; }

    public async Task DevLaunchAsync(bool secondClient)
    {
        IsBusy = true;

        try
        {
            var options = FiveMLaunchOptions.Create(
                address: null,
                gameClient: DevClient,
                gameBuild: _settings.DevGameBuild,
                pureMode: _settings.DevPureMode,
                secondClient: secondClient);

            var result = await _launcher.OpenAsync(options);
            StatusText = Describe(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public ObservableCollection<SavedServerItem> SavedServers { get; }

    public ICollectionView SavedServersView { get; }

    private string _serverSearchText = string.Empty;

    public string ServerSearchText
    {
        get => _serverSearchText;
        set
        {
            if (SetProperty(ref _serverSearchText, value))
            {
                SavedServersView.Refresh();
            }
        }
    }

    private bool MatchesServerSearch(object item)
    {
        if (string.IsNullOrWhiteSpace(_serverSearchText))
        {
            return true;
        }

        var server = (SavedServerItem)item;
        return server.Name.Contains(_serverSearchText, StringComparison.OrdinalIgnoreCase)
               || server.Address.Contains(_serverSearchText, StringComparison.OrdinalIgnoreCase);
    }

    public ObservableCollection<InstalledClientOption> AvailableOpenClients { get; }

    public CfxStatusItem FiveMStatus { get; }

    public CfxStatusItem FiveMEnhancedStatus { get; }

    public CfxStatusItem RedMStatus { get; }

    public IReadOnlyList<CfxStatusItem> CfxStatuses { get; }

    public InstalledClientOption? SelectedOpenClient
    {
        get => _selectedOpenClient;
        set
        {
            if (ReferenceEquals(_selectedOpenClient, value))
            {
                return;
            }

            _selectedOpenClient = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedOpenClient)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedOpenClientLabel)));
        }
    }

    public string SelectedOpenClientLabel => SelectedOpenClient?.DisplayName ?? string.Empty;

    public bool AutoLaunch
    {
        get => _settings.AutoLaunch;
        set
        {
            if (_settings.AutoLaunch == value)
            {
                return;
            }

            _settings.AutoLaunch = value;
            SaveSettings();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoLaunch)));
        }
    }

    public InstalledClientOption? PreferredClientOption
    {
        get => _preferredClientOption;
        set
        {
            if (ReferenceEquals(_preferredClientOption, value) || value is null)
            {
                return;
            }

            _preferredClientOption = value;
            _settings.PreferredClient = value.Client;
            SaveSettings();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreferredClientOption)));
        }
    }

    public SavedServerItem? SelectedServer
    {
        get => _selectedServer;
        set
        {
            if (ReferenceEquals(_selectedServer, value))
            {
                return;
            }

            _selectedServer = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedServer)));

            if (value is not null)
            {
                ServerAddress = value.Address;
            }
        }
    }

    private void OpenAddServerDialog(object? _)
    {
        OpenAddServerDialog();
    }

    private void OpenAddServerDialog()
    {
        EditingServer = null;
        DialogServerName = string.Empty;
        DialogServerAddress = string.Empty;
        DialogRequiresSteam = false;
        DialogRequiresDiscord = false;
        DialogError = string.Empty;
        IsServerDialogOpen = true;
    }

    private void OpenEditServerDialog(object? row)
    {
        if (row is not SavedServerItem item)
        {
            return;
        }

        EditingServer = item;
        DialogServerName = item.Name;
        DialogServerAddress = item.Address;
        DialogRequiresSteam = item.RequiresSteam;
        DialogRequiresDiscord = item.RequiresDiscord;
        DialogError = string.Empty;
        IsServerDialogOpen = true;
    }

    private void CloseServerDialog(object? _ = null)
    {
        IsServerDialogOpen = false;
        EditingServer = null;
    }

    private async Task SaveServerDialogAsync()
    {
        var addressUnchanged = EditingServer is not null
            && string.Equals(EditingServer.Address, DialogServerAddress.Trim(), StringComparison.OrdinalIgnoreCase);

        var cfxId = addressUnchanged ? EditingServer!.CfxId : null;
        SavedServer savedServer;

        try
        {
            savedServer = SavedServer.Create(
                DialogServerName,
                DialogServerAddress,
                DialogRequiresSteam,
                DialogRequiresDiscord,
                cfxId);
        }
        catch (ArgumentException)
        {
            DialogError = "Invalid name or address";
            return;
        }

        var conflicting = _serverRepository.FindByAddress(savedServer.Address);

        if (EditingServer is null)
        {
            if (conflicting is not null)
            {
                DialogError = "Server already saved";
                return;
            }

            _serverRepository.Add(savedServer);
            var item = ToItem(savedServer);
            SavedServers.Add(item);
            TryCaptureCfxId(item);
        }
        else
        {
            if (conflicting is not null
                && !conflicting.MatchesAddress(EditingServer.Address))
            {
                DialogError = "Server already saved";
                return;
            }

            var oldRow = SavedServers.FirstOrDefault(s =>
                string.Equals(s.Address, EditingServer.Address, StringComparison.OrdinalIgnoreCase));

            if (conflicting is null
                && string.Equals(EditingServer.Address, savedServer.Address, StringComparison.OrdinalIgnoreCase))
            {
                _serverRepository.Update(savedServer);
            }
            else
            {
                _serverRepository.Remove(EditingServer.Address);
                _serverRepository.Add(savedServer);
            }

            if (oldRow is not null)
            {
                var index = SavedServers.IndexOf(oldRow);
                SavedServers.RemoveAt(index);
                var item = ToItem(savedServer);
                SavedServers.Insert(index, item);
                TryCaptureCfxId(item);
            }
        }

        await WaitForPendingCapturesAsync();

        try
        {
            await RefreshServerInfoAsync(forceRefresh: true);
        }
        catch (Exception)
        {
            // Refresh failure must not fail the save itself.
        }

        StatusText = "Server saved";
        CloseServerDialog();
    }

    private void TryCaptureCfxId(SavedServerItem item)
    {
        if (_serverRepository.FindByAddress(item.Address)?.CfxId is not null)
        {
            return;
        }

        lock (_captureGate)
        {
            var capture = CaptureCfxIdAsync(item);
            _pendingCaptures.Add(capture);
            _ = capture.ContinueWith(t =>
            {
                lock (_captureGate)
                {
                    _pendingCaptures.Remove(capture);
                }
            }, TaskScheduler.Default);
        }
    }

    private async Task CaptureCfxIdAsync(SavedServerItem item)
    {
        var cfxId = await _enrichment.ResolveCfxIdAsync(item.Address);

        if (cfxId is null)
        {
            return;
        }

        var saved = _serverRepository.FindByAddress(item.Address);

        if (saved is null)
        {
            return;
        }

        var updated = SavedServer.Create(saved.Name, saved.Address, saved.RequiresSteam, saved.RequiresDiscord, cfxId);
        _serverRepository.Update(updated);
        item.SetCfxId(cfxId);
    }

    public async Task RefreshServerInfoAsync(bool forceRefresh = false)
    {
        var presence = await _enrichment.RefreshAsync(forceRefresh);

        if (presence is null)
        {
            return;
        }

        foreach (var item in SavedServers)
        {
            if (!item.HasCfxId)
            {
                continue;
            }

            if (presence.TryGetValue(item.CfxId!, out var serverPresence))
            {
                item.ApplyPresence(serverPresence);
            }
            else
            {
                item.ApplyPresence(new ServerPresence(false, 0, 0, null));
            }

            var icon = await _enrichment.GetIconAsync(item.CfxId!);

            if (icon is not null && item.Icon != icon)
            {
                item.SetIcon(icon);
            }
        }
    }

    public async Task RefreshCfxStatusAsync()
    {
        var statuses = await _cfxStatus.GetStatusesAsync();

        if (statuses is null)
        {
            return;
        }

        FiveMStatus.Apply(statuses.GetValueOrDefault(GameClient.FiveM, CfxStatus.Unknown));
        FiveMEnhancedStatus.Apply(statuses.GetValueOrDefault(GameClient.FiveMEnhanced, CfxStatus.Unknown));
        RedMStatus.Apply(statuses.GetValueOrDefault(GameClient.RedM, CfxStatus.Unknown));
    }

    public async Task WaitForPendingCapturesAsync()
    {
        while (true)
        {
            Task[] snapshot;
            lock (_captureGate)
            {
                snapshot = _pendingCaptures.ToArray();
            }

            if (snapshot.Length == 0)
            {
                return;
            }

            await Task.WhenAll(snapshot);
        }
    }

    public async Task RunEnrichmentLoopAsync(
        CancellationToken cancellationToken,
        TimeSpan? cadence = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var interval = cadence ?? TimeSpan.FromMinutes(1);
        var wait = delay ?? ((duration, token) => Task.Delay(duration, token));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshServerInfoAsync();
                await RefreshCfxStatusAsync();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
            }

            try
            {
                await wait(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            SetProperty(ref _isBusy, value);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private SavedServerItem ToItem(SavedServer savedServer)
    {
        SavedServerItem item = null!;

        item = new SavedServerItem(
            savedServer.Name,
            savedServer.Address,
            savedServer.RequiresSteam,
            savedServer.RequiresDiscord,
            () => PersistServer(item),
            savedServer.CfxId);

        return item;
    }

    private void PersistServer(SavedServerItem item)
    {
        _serverRepository.Update(SavedServer.Create(item.Name, item.Address, item.RequiresSteam, item.RequiresDiscord, item.CfxId));
    }

    private void DeleteServer()
    {
        if (SelectedServer is null)
        {
            return;
        }

        _serverRepository.Remove(SelectedServer.Address);
        SavedServers.Remove(SelectedServer);
        SelectedServer = null;
    }

    public async Task ConnectAsync()
    {
        IsBusy = true;
        StatusText = "Resolving...";

        try
        {
            var savedServer = _serverRepository.FindByAddress(ServerAddress);
            var profile = await _resolver.ResolveAsync(ServerAddress, savedServer);

            foreach (var app in RequiredApps(profile.Requirements))
            {
                StatusText = $"Starting {app}...";

                if (!await _preparer.TryPrepareAsync(app))
                {
                    StatusText = $"Could not start {app}";
                    return;
                }
            }

            var result = await _launcher.ConnectAsync(profile);

            StatusText = Describe(result);

            if (result is LaunchResult.Connect or LaunchResult.OpenClient)
            {
                _settings.LastServerAddress = ServerAddress.Trim();
                SaveSettings();
            }
        }
        catch (InvalidAddressException)
        {
            StatusText = "Invalid address";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static List<ExternalApp> RequiredApps(ServerRequirements requirements)
    {
        var apps = new List<ExternalApp>();

        if (requirements.SteamRequired == true)
        {
            apps.Add(ExternalApp.Steam);
        }

        if (requirements.DiscordRequired == true)
        {
            apps.Add(ExternalApp.Discord);
        }

        return apps;
    }

    private bool CanConnect()
    {
        return !string.IsNullOrWhiteSpace(ServerAddress) && !IsBusy;
    }

    private bool CanDeleteServer()
    {
        return SelectedServer is not null;
    }

    public async Task InitializeAsync()
    {
        AvailableOpenClients.Clear();

        foreach (var client in OpenCandidates)
        {
            if (await _installLocator.IsInstalledAsync(client))
            {
                AvailableOpenClients.Add(new InstalledClientOption(client));
            }
        }

        if (AvailableOpenClients.Count > 0)
        {
            var preferred = AvailableOpenClients.FirstOrDefault(o => o.Client == _settings.PreferredClient);
            SelectedOpenClient = preferred ?? AvailableOpenClients[0];
            _preferredClientOption = SelectedOpenClient;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreferredClientOption)));
        }

        await RefreshCfxStatusAsync();

        if (_settings.AutoLaunch && !string.IsNullOrWhiteSpace(_settings.LastServerAddress))
        {
            ServerAddress = _settings.LastServerAddress;
            await ConnectAsync();
        }
    }

    public async Task OpenClientAsync()
    {
        if (SelectedOpenClient is not { } option)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _launcher.OpenAsync(option.Client);
            StatusText = Describe(result);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanOpenClient()
    {
        return SelectedOpenClient is not null && !IsBusy;
    }

    private void SelectOpenClient(object? option)
    {
        if (option is InstalledClientOption installed)
        {
            SelectedOpenClient = installed;
        }
    }

    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;

        if (IsSettingsOpen)
        {
            IsDevMode = false;
        }
    }

    private void ToggleDevMode()
    {
        IsDevMode = !IsDevMode;

        if (IsDevMode)
        {
            IsSettingsOpen = false;
        }
    }

    private void SaveSettings()
    {
        _settingsRepository.Save(_settings);
    }

    private static string Describe(LaunchResult result)
    {
        return result switch
        {
            LaunchResult.Connect => "Launching FiveM...",
            LaunchResult.OpenClient(var client) => $"Opening {InstalledClientOption.DisplayNameOf(client)}...",
            LaunchResult.NotInstalled(var client) => $"{InstalledClientOption.DisplayNameOf(client)} is not installed",
            LaunchResult.StartFailed => "Launch failed",
            _ => throw new InvalidOperationException("Unknown LaunchResult"),
        };
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
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