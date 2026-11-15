using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Launch;
using FiveMServerLauncher.Localization;
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
    private readonly ILocalizer _localizer;
    private readonly object _captureGate = new();
    private readonly List<Task> _pendingCaptures = [];
    private LanguageSettingOption _selectedLanguageOption;

    private LauncherSettings _settings;
    private readonly IReleaseFeed? _releaseFeed;
    private readonly IUpdateApplier? _updateApplier;
    private readonly ShortcutDependencies? _shortcuts;
    private readonly LauncherSchemeRegistrar? _webLinks;
    private LauncherUpdate? _latestUpdate;
    private bool _isUpdateAvailable;
    private bool _isUpdateInProgress;
    private bool _isUpdateFailed;

    private string _serverAddress = string.Empty;
    private string _statusText = string.Empty;
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
        ServerBrowserViewModel browserViewModel,
        ILocalizer localizer,
        TimeSpan? refreshCooldown = null,
        CancellationToken? cancellationToken = null,
        IReleaseFeed? releaseFeed = null,
        IUpdateApplier? updateApplier = null,
        ShortcutDependencies? shortcuts = null,
        LauncherSchemeRegistrar? webLinks = null)
    {
        _resolver = resolver;
        _launcher = launcher;
        _serverRepository = serverRepository;
        _preparer = preparer;
        _installLocator = installLocator;
        _settingsRepository = settingsRepository;
        _enrichment = enrichment;
        _cfxStatus = cfxStatus;
        _localizer = localizer;
        Browser = browserViewModel;
        _refreshCooldown = refreshCooldown ?? DefaultRefreshCooldown;
        _cancellationToken = cancellationToken ?? CancellationToken.None;
        _releaseFeed = releaseFeed;
        _updateApplier = updateApplier;
        _shortcuts = shortcuts;
        _webLinks = webLinks;
        _settings = settingsRepository.Load();
        LanguageOptions = BuildLanguageOptions();
        _selectedLanguageOption = LanguageOptions.FirstOrDefault(o => o.Tag == _settings.Language)
            ?? LanguageOptions[0];
        // Subscribed before SetLanguage: applying the persisted language raises
        // LanguageChanged, and the option labels are localized strings built from
        // it — subscribing afterwards would leave them in the previous language.
        _localizer.LanguageChanged += OnLanguageChanged;
        _localizer.SetLanguage(_settings.Language);
        StatusText = _localizer.Get("StatusReady");
        FiveMStatus = new CfxStatusItem(GameClient.FiveM, _localizer);
        FiveMEnhancedStatus = new CfxStatusItem(GameClient.FiveMEnhanced, _localizer);
        RedMStatus = new CfxStatusItem(GameClient.RedM, _localizer);
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect, OnCommandFailed);
        DeleteServerCommand = new RelayCommand(DeleteServer);
        CreateDesktopShortcutCommand = new RelayCommand(CreateDesktopShortcut);
        MoveServerUpCommand = new RelayCommand(p => MoveServerBy(p as SavedServerItem, -1), () => IsReorderAvailable);
        MoveServerDownCommand = new RelayCommand(p => MoveServerBy(p as SavedServerItem, 1), () => IsReorderAvailable);
        OpenClientCommand = new AsyncRelayCommand(OpenClientAsync, CanOpenClient, OnCommandFailed);
        SelectOpenClientCommand = new RelayCommand(SelectOpenClient);
        SettingsCommand = new RelayCommand(ToggleSettings);
        ToggleDevModeCommand = new RelayCommand(ToggleDevMode);
        OpenAddServerDialogCommand = new RelayCommand(OpenAddServerDialog);
        OpenEditServerDialogCommand = new RelayCommand(OpenEditServerDialog);
        SaveServerDialogCommand = new AsyncRelayCommand(SaveServerDialogAsync, onError: OnCommandFailed);
        CancelServerDialogCommand = new RelayCommand(CloseServerDialog);
        ConfirmCommand = new RelayCommand(ConfirmPendingAction);
        CancelConfirmCommand = new RelayCommand(CloseConfirm);
        RefreshServersCommand = new AsyncRelayCommand(RefreshServersAsync, () => !IsRefreshingServers, OnCommandFailed);
        OpenServerBrowserCommand = new AsyncRelayCommand(OpenServerBrowserAsync, onError: OnCommandFailed);
        CloseServerBrowserCommand = new RelayCommand(_ => IsServerBrowserOpen = false);
        ConnectBrowserServerCommand = new AsyncRelayCommand(p => ConnectFromBrowserAsync((ServerBrowserItem)p!), onError: OnCommandFailed);
        SaveBrowserServerCommand = new RelayCommand(p =>
        {
            if (p is ServerBrowserItem item)
            {
                SaveBrowserServer(item);
            }
        });
        ToggleDevClientCommand = new RelayCommand(() =>
            DevClient = DevClient == GameClient.FiveM ? GameClient.RedM : GameClient.FiveM);
        DevLaunchCommand = new AsyncRelayCommand((object? secondClient) => DevLaunchAsync(secondClient is true), () => !IsBusy, OnCommandFailed);
        UpdateCommand = new AsyncRelayCommand(ApplyUpdateAsync, CanApplyUpdate, OnCommandFailed);
        DismissUpdateCommand = new RelayCommand(DismissUpdate, CanDismissUpdate);

        SavedServers = new ObservableCollection<SavedServerItem>(
            serverRepository.GetAll().Select(ToItem));
        SavedServersView = CollectionViewSource.GetDefaultView(SavedServers);
        SavedServersView.Filter = MatchesServerSearch;
        AvailableOpenClients = new ObservableCollection<InstalledClientOption>();
        UpdateRowMoveStates();

        CfxStatuses = [FiveMStatus, FiveMEnhancedStatus, RedMStatus];
    }

    public ICommand ConnectCommand { get; }

    public ICommand DeleteServerCommand { get; }

    public ICommand CreateDesktopShortcutCommand { get; }

    public ICommand MoveServerUpCommand { get; }

    public ICommand MoveServerDownCommand { get; }

    // Reordering is a whole-list operation, so it is only available when the visible
    // order is the real order: no search filter (a filtered view is a non-contiguous
    // subset, where "move down one" would mean two real positions) and more than one
    // row to swap.
    public bool IsReorderAvailable => SavedServers.Count > 1
                                      && string.IsNullOrWhiteSpace(_serverSearchText);

    public ICommand OpenClientCommand { get; }

    public ICommand SelectOpenClientCommand { get; }

    public ICommand SettingsCommand { get; }

    public ICommand OpenAddServerDialogCommand { get; }

    public ICommand OpenEditServerDialogCommand { get; }

    public ICommand SaveServerDialogCommand { get; }

    public ICommand CancelServerDialogCommand { get; }

    public ICommand ConfirmCommand { get; }

    public ICommand CancelConfirmCommand { get; }

    public ICommand RefreshServersCommand { get; }

    public ServerBrowserViewModel Browser { get; }

    public ICommand OpenServerBrowserCommand { get; }

    public ICommand CloseServerBrowserCommand { get; }

    public ICommand ConnectBrowserServerCommand { get; }

    public ICommand SaveBrowserServerCommand { get; }

    public ICommand UpdateCommand { get; }

    public ICommand DismissUpdateCommand { get; }

    private bool _isServerBrowserOpen;

    public bool IsServerBrowserOpen
    {
        get => _isServerBrowserOpen;
        private set => SetProperty(ref _isServerBrowserOpen, value);
    }

    private async Task OpenServerBrowserAsync()
    {
        IsSettingsOpen = false;
        IsDevMode = false;
        CloseServerDialog();
        IsServerBrowserOpen = true;
        await Browser.LoadAsync();
    }

    public async Task ConnectFromBrowserAsync(ServerBrowserItem item)
    {
        IsServerBrowserOpen = false;
        ServerAddress = Domain.ServerAddress.FromCfxId(item.CfxId);
        await ConnectAsync();
    }

    // Rebuilt every time the tray menu opens, from the full saved list (never the
    // search-filtered view), so it follows adds, removes, reorders and renames for free.
    public IReadOnlyList<TrayMenuEntry> BuildTrayMenu() => TrayMenu.Build(SavedServers, IsBusy, _localizer);

    public async Task HandleForwardedConnectAsync(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || IsBusy)
        {
            return;
        }

        ServerAddress = address;
        await ConnectAsync();
    }

    private bool _isRefreshingServers;

    private static readonly TimeSpan DefaultRefreshCooldown = TimeSpan.FromSeconds(15);
    private readonly TimeSpan _refreshCooldown;
    private readonly CancellationToken _cancellationToken;

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
            await RefreshServerInfoAsync(forceRefresh: true, _cancellationToken);
        }
        catch (Exception)
        {
            // A failed manual refresh leaves rows untouched.
        }

        // Keep the command disabled for the shared cooldown so the expensive
        // catalog download can't be spammed (scrolling must not re-enable it).
        if (await Cooldown.ElapseAsync(_refreshCooldown, _cancellationToken))
        {
            IsRefreshingServers = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private SavedServerItem? _editingServer;
    private bool _isServerDialogOpen;
    private string _dialogServerName = string.Empty;
    private string _dialogServerAddress = string.Empty;
    private bool _dialogRequiresSteam;
    private bool _dialogRequiresDiscord;
    private string _dialogError = string.Empty;
    private string _dialogCfxId = string.Empty;
    private int? _dialogGameBuild;
    private int? _dialogPureMode;
    private GameClient? _dialogGameClient;
    private bool _isDialogLocalhost;
    private bool _isConfirmOpen;
    private string _confirmText = string.Empty;
    private Action? _pendingConfirmAction;

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

    public string ServerDialogTitle => EditingServer is null
        ? _localizer.Get("DialogNewServer")
        : _localizer.Get("DialogEditServer");

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
        set
        {
            if (SetProperty(ref _dialogServerAddress, value))
            {
                UpdateIsDialogLocalhost();
            }
        }
    }

    public bool IsDialogLocalhost
    {
        get => _isDialogLocalhost;
        private set => SetProperty(ref _isDialogLocalhost, value);
    }

    public bool IsConfirmOpen
    {
        get => _isConfirmOpen;
        private set => SetProperty(ref _isConfirmOpen, value);
    }

    public string ConfirmText
    {
        get => _confirmText;
        private set => SetProperty(ref _confirmText, value);
    }

    public void RequestConfirmation(string text, Action onConfirm)
    {
        _pendingConfirmAction = onConfirm;
        ConfirmText = text;
        IsConfirmOpen = true;
    }

    private void ConfirmPendingAction()
    {
        var action = _pendingConfirmAction;
        CloseConfirm();
        action?.Invoke();
    }

    private void CloseConfirm()
    {
        _pendingConfirmAction = null;
        IsConfirmOpen = false;
    }

    public string DialogCfxId
    {
        get => _dialogCfxId;
        set => SetProperty(ref _dialogCfxId, value);
    }

    public int? DialogGameBuild
    {
        get => _dialogGameBuild;
        set
        {
            if (SetProperty(ref _dialogGameBuild, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DialogGameBuildOptions)));
            }
        }
    }

    public IReadOnlyList<GameBuildOption> DialogGameBuildOptions =>
        DialogGameClient is GameClient game
            ? GameBuilds.Options(game, _dialogGameBuild, _localizer.Get("GameClientNone"), _gameBuildData)
            : [];

    public bool IsDialogGameBuildEnabled => DialogGameClient is GameClient game && GameBuilds.SupportsBuildSelection(game);

    public int? DialogPureMode
    {
        get => _dialogPureMode;
        set => SetProperty(ref _dialogPureMode, value);
    }

    public GameClient? DialogGameClient
    {
        get => _dialogGameClient;
        set
        {
            if (SetProperty(ref _dialogGameClient, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DialogGameBuildOptions)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDialogGameBuildEnabled)));
            }
        }
    }

    public IReadOnlyList<GameClientOption> DialogGameClientOptions =>
        [
            new(null, _localizer.Get("GameClientNone")),
            new(GameClient.FiveM, InstalledClientOption.DisplayNameOf(GameClient.FiveM)),
            new(GameClient.FiveMEnhanced, InstalledClientOption.DisplayNameOf(GameClient.FiveMEnhanced)),
            new(GameClient.RedM, InstalledClientOption.DisplayNameOf(GameClient.RedM))
        ];


    private void UpdateIsDialogLocalhost()
    {
        var trimmed = _dialogServerAddress.Trim();
        IsDialogLocalhost = trimmed.Length > 0 && Domain.ServerAddress.IsLoopbackAddress(trimmed);
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
    private GameBuildData? _gameBuildData;

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevGameBuildOptions)));
            }
        }
    }

    public string DevClientLabel => InstalledClientOption.DisplayNameOf(DevClient);

    public string DevClientButtonText => _localizer.Format("DevClientButton", DevClientLabel);

    public ICommand ToggleDevClientCommand { get; }

    public int? DevGameBuild
    {
        get => _settings.DevGameBuild;
        set
        {
            if (_settings.DevGameBuild == value)
            {
                return;
            }

            var previous = _settings.DevGameBuild;
            _settings.DevGameBuild = value;
            TrySaveSettings(() => _settings.DevGameBuild = previous);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevGameBuild)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevGameBuildOptions)));
        }
    }

    public IReadOnlyList<GameBuildOption> DevGameBuildOptions =>
        GameBuilds.Options(DevClient, _settings.DevGameBuild, _localizer.Get("GameClientNone"), _gameBuildData);

    public void SetGameBuildData(GameBuildData data)
    {
        _gameBuildData = data;

        foreach (var item in SavedServers)
        {
            item.SetGameBuildData(data);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevGameBuildOptions)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DialogGameBuildOptions)));
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

            var previous = _settings.DevPureMode;
            _settings.DevPureMode = value;
            TrySaveSettings(() => _settings.DevPureMode = previous);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevPureMode)));
        }
    }

    public ICommand DevLaunchCommand { get; }
    public Task DevLaunchAsync(bool secondClient)
    {
        return RunBusyFlowAsync(async () =>
        {
            var options = FiveMLaunchOptions.Create(
                address: null,
                gameClient: DevClient,
                gameBuild: _settings.DevGameBuild,
                pureMode: _settings.DevPureMode,
                secondClient: secondClient);

            var result = await _launcher.OpenAsync(options);
            StatusText = Describe(result);
        });
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
                // The reorder gates reopen (or close) once the hidden rows are
                // visible again.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReorderAvailable)));
                UpdateRowMoveStates();
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

    public IReadOnlyList<LanguageSettingOption> LanguageOptions { get; private set; }

    public LanguageSettingOption SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (value is null || ReferenceEquals(_selectedLanguageOption, value))
            {
                return;
            }

            var previousOption = _selectedLanguageOption;
            var previousTag = _settings.Language;
            _selectedLanguageOption = value;
            _settings.Language = value.Tag;
            var saved = TrySaveSettings(() =>
            {
                _selectedLanguageOption = previousOption;
                _settings.Language = previousTag;
            });
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLanguageOption)));

            if (saved)
            {
                _localizer.SetLanguage(value.Tag);
            }
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selectedTag = _selectedLanguageOption.Tag;
        LanguageOptions = BuildLanguageOptions();
        _selectedLanguageOption = LanguageOptions.FirstOrDefault(o => o.Tag == selectedTag)
            ?? LanguageOptions[0];

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LanguageOptions)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLanguageOption)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerDialogTitle)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevClientButtonText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DialogGameClientOptions)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DevGameBuildOptions)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DialogGameBuildOptions)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateBannerText)));
    }

    private IReadOnlyList<LanguageSettingOption> BuildLanguageOptions()
    {
        var options = new List<LanguageSettingOption> { new(null, _localizer.Get("LanguageSystemDefault")) };
        options.AddRange(_localizer.Languages.Select(l => new LanguageSettingOption(l.Tag, l.DisplayName)));
        return options;
    }

    public bool AutoLaunch
    {
        get => _settings.AutoLaunch;
        set => SetPersistedFlag(_settings.AutoLaunch, value, v => _settings.AutoLaunch = v);
    }

    // A plain persisted checkbox: apply, save, and on a failed save revert to the old value
    // (TrySaveSettings reports the status); PropertyChanged fires either way so the
    // checkbox shows what the settings really hold.
    private void SetPersistedFlag(bool current, bool value, Action<bool> apply, [CallerMemberName] string? propertyName = null)
    {
        if (current == value)
        {
            return;
        }

        apply(value);
        TrySaveSettings(() => apply(current));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Read by the tray window controller on every minimize; Settings checkbox.
    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set => SetPersistedFlag(_settings.MinimizeToTray, value, v => _settings.MinimizeToTray = v);
    }

    /// <summary>
    /// Opt-in cfxlauncher:// registration. The registry change runs first (a refusal or
    /// fault reverts with its own status); a settings save failure then undoes it
    /// best-effort, so registry and setting never disagree. Unwired registrar = persist only.
    /// </summary>
    public bool WebLinks
    {
        get => _settings.WebLinks;
        set
        {
            if (_settings.WebLinks == value)
            {
                return;
            }

            if (!ApplyWebLinks(value))
            {
                StatusText = _localizer.Get("StatusWebLinksFailed");
            }
            else
            {
                _settings.WebLinks = value;
                TrySaveSettings(() =>
                {
                    _settings.WebLinks = !value;
                    ApplyWebLinks(!value);
                });
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WebLinks)));
        }
    }

    private bool ApplyWebLinks(bool enabled)
    {
        return _webLinks is not { } registrar || (enabled ? registrar.TryEnable() : registrar.TryDisable());
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

            var previousOption = _preferredClientOption;
            var previousClient = _settings.PreferredClient;
            _preferredClientOption = value;
            _settings.PreferredClient = value.Client;
            TrySaveSettings(() =>
            {
                _preferredClientOption = previousOption;
                _settings.PreferredClient = previousClient;
            });
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

            foreach (var item in SavedServers)
            {
                item.IsSelected = ReferenceEquals(item, value);
            }

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
        DialogCfxId = string.Empty;
        DialogGameBuild = null;
        DialogPureMode = null;
        DialogGameClient = null;
        DialogError = string.Empty;
        IsServerDialogOpen = true;
    }

    public void SaveBrowserServer(ServerBrowserItem item)
    {
        if (item.IsSaved)
        {
            return;
        }

        SavedServer savedServer;
        try
        {
            // The cfx id is the durable connect address: a saved browser server keeps working
            // when the server moves host, unlike its raw connect endpoint.
            savedServer = SavedServer.Create(
                item.Name, Domain.ServerAddress.FromCfxId(item.CfxId), cfxId: item.CfxId);
        }
        catch (ArgumentException)
        {
            return;
        }

        if (_serverRepository.FindByAddress(savedServer.Address) is not null
            || _serverRepository.FindByCfxId(item.CfxId) is not null)
        {
            item.SetSaved(true);
            return;
        }

        if (AddSavedServerRow(savedServer))
        {
            item.SetSaved(true);
        }
    }

    private bool AddSavedServerRow(SavedServer savedServer)
    {
        if (!PersistOrReport(() => _serverRepository.Add(savedServer)))
        {
            return false;
        }

        var row = ToItem(savedServer);
        SavedServers.Add(row);
        UpdateRowMoveStates();
        TryCaptureCfxId(row);
        return true;
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
        DialogCfxId = item.CfxId ?? string.Empty;
        DialogGameBuild = item.GameBuild;
        DialogPureMode = item.PureMode;
        DialogGameClient = item.GameClient;
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

        string? cfxId;
        int? gameBuild = null;
        int? pureMode = null;
        GameClient? gameClient = null;

        if (IsDialogLocalhost)
        {
            var trimmedCfxId = DialogCfxId.Trim();
            cfxId = trimmedCfxId.Length > 0 ? trimmedCfxId : null;
            gameBuild = DialogGameBuild;
            pureMode = DialogPureMode;
            gameClient = DialogGameClient;
        }
        else
        {
            cfxId = addressUnchanged ? EditingServer!.CfxId : null;
        }

        SavedServer savedServer;

        try
        {
            savedServer = SavedServer.Create(
                DialogServerName,
                DialogServerAddress,
                DialogRequiresSteam,
                DialogRequiresDiscord,
                cfxId,
                gameBuild,
                pureMode,
                gameClient);
        }
        catch (ArgumentException)
        {
            DialogError = _localizer.Get("DialogInvalidNameOrAddress");
            return;
        }

        var conflicting = _serverRepository.FindByAddress(savedServer.Address);

        if (EditingServer is null)
        {
            if (conflicting is not null)
            {
                DialogError = _localizer.Get("DialogServerAlreadySaved");
                return;
            }

            if (!AddSavedServerRow(savedServer))
            {
                DialogError = _localizer.Get("StatusSaveFailed");
                return;
            }
        }
        else
        {
            if (conflicting is not null
                && !conflicting.MatchesAddress(EditingServer.Address))
            {
                DialogError = _localizer.Get("DialogServerAlreadySaved");
                return;
            }

            var oldRow = SavedServers.FirstOrDefault(s =>
                string.Equals(s.Address, EditingServer.Address, StringComparison.OrdinalIgnoreCase));

            var editedAddress = EditingServer.Address;

            // An edit that changes the address is still an edit, not a remove+add:
            // the server keeps its slot in the list and the store.
            var persisted = string.Equals(editedAddress, savedServer.Address, StringComparison.OrdinalIgnoreCase)
                ? PersistOrReport(() => _serverRepository.Update(savedServer))
                : PersistOrReport(() => _serverRepository.Replace(editedAddress, savedServer));

            if (!persisted)
            {
                DialogError = _localizer.Get("StatusSaveFailed");
                return;
            }

            if (oldRow is not null)
            {
                var index = SavedServers.IndexOf(oldRow);
                SavedServers.RemoveAt(index);
                var item = ToItem(savedServer);
                SavedServers.Insert(index, item);
                UpdateRowMoveStates();
                TryCaptureCfxId(item);
            }
        }

        await WaitForPendingCapturesAsync();

        try
        {
            await RefreshServerInfoAsync(forceRefresh: true, _cancellationToken);
        }
        catch (Exception)
        {
            // Refresh failure must not fail the save itself.
        }

        StatusText = _localizer.Get("StatusServerSaved");
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
        try
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

            var updated = SavedServer.Create(
                saved.Name, saved.Address, saved.RequiresSteam, saved.RequiresDiscord, cfxId,
                saved.GameBuild, saved.PureMode, saved.GameClient);
            _serverRepository.Update(updated);
            item.SetCfxId(cfxId);
        }
        catch (Exception ex) when (IsStoreFault(ex))
        {
            // A background capture has no one to tell; the id is simply captured next save.
        }
    }

    public async Task RefreshServerInfoAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var presence = await _enrichment.RefreshAsync(forceRefresh);

        if (presence is null)
        {
            return;
        }

        foreach (var item in SavedServers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!item.HasCfxId)
            {
                continue;
            }

            var serverPresence = presence.TryGetValue(item.CfxId!, out var found)
                ? found
                : ServerPresence.Offline;

            item.ApplyPresence(serverPresence);

            // The snapshot either publishes the icon URL or proves there is none,
            // so the loop never needs to probe /single/ for it.
            if (serverPresence.IconUrl is null)
            {
                continue;
            }

            var icon = await _enrichment.GetIconAsync(serverPresence.IconUrl);

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

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set => SetProperty(ref _isUpdateAvailable, value);
    }

    public bool IsUpdateInProgress
    {
        get => _isUpdateInProgress;
        private set => SetProperty(ref _isUpdateInProgress, value);
    }

    public bool IsUpdateFailed
    {
        get => _isUpdateFailed;
        private set => SetProperty(ref _isUpdateFailed, value);
    }

    public string UpdateBannerText => _latestUpdate is null
        ? string.Empty
        : _localizer.Format("UpdateBannerText", _latestUpdate.Tag);

    private async Task CheckForUpdateAsync()
    {
        // Both seams or none: the check only runs when the whole update feature is wired
        // (nulls mean a test or embedding declined the feature).
        if (_releaseFeed is null || _updateApplier is null)
        {
            return;
        }

        try
        {
            if (await _releaseFeed.CheckForUpdateAsync() is { } update)
            {
                _latestUpdate = update;
                IsUpdateAvailable = true;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpdateBannerText)));
            }
        }
        catch (Exception)
        {
            // A failing update check never breaks startup and never shows a banner.
        }
    }

    public async Task ApplyUpdateAsync()
    {
        if (_latestUpdate is not { } update
            || _updateApplier is not { } applier
            || !CanApplyUpdate())
        {
            return;
        }

        IsUpdateAvailable = false;
        IsUpdateInProgress = true;

        try
        {
            // true means the applier relaunched and already exited the app — no state
            // change follows. false (or a throw) leaves a dismissable failed banner.
            if (!await applier.ApplyAsync(update))
            {
                IsUpdateInProgress = false;
                IsUpdateFailed = true;
            }
        }
        catch (Exception)
        {
            IsUpdateInProgress = false;
            IsUpdateFailed = true;
        }
    }

    private bool CanApplyUpdate()
    {
        return IsUpdateAvailable && !IsUpdateInProgress;
    }

    private bool CanDismissUpdate()
    {
        // Dismiss is a promise that nothing re-shows the banner this session; it is only
        // honest from the available/failed states. Mid-flight the handoff is underway and a
        // later failure would break that promise, so dismissal is refused.
        return !IsUpdateInProgress;
    }

    private void DismissUpdate()
    {
        if (!CanDismissUpdate())
        {
            return;
        }

        IsUpdateAvailable = false;
        IsUpdateInProgress = false;
        IsUpdateFailed = false;
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

    // Completed while the window is attended; a fresh, pending source while it is away
    // (minimized or inactive). The enrichment loop parks on it before every cycle, so an
    // unseen window downloads no catalog, and the return releases the parked cycle at once.
    private TaskCompletionSource _windowAttended = CompletedAttendance();

    private static TaskCompletionSource CompletedAttendance()
    {
        var attended = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        attended.SetResult();
        return attended;
    }

    public void SetWindowAttended(bool attended)
    {
        var current = Volatile.Read(ref _windowAttended);

        if (attended)
        {
            current.TrySetResult();
        }
        else if (current.Task.IsCompleted)
        {
            Volatile.Write(
                ref _windowAttended,
                new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
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
                await Volatile.Read(ref _windowAttended).Task.WaitAsync(cancellationToken);
                await RefreshServerInfoAsync(cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
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

    // The last-resort entry point for the composition root's unhandled-exception hooks:
    // the app stays up and says so in one generic line (no log file by design).
    public void ReportUnexpectedError()
    {
        StatusText = _localizer.Get("StatusUnexpectedError");
    }

    // Every async command's swallowed fault lands here: the app stays up (AsyncRelayCommand
    // never throws) and the status line says something went wrong.
    private void OnCommandFailed(Exception exception)
    {
        ReportUnexpectedError();
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
            _localizer,
            savedServer.CfxId,
            savedServer.GameBuild,
            savedServer.PureMode,
            savedServer.GameClient,
            _gameBuildData);

        return item;
    }

    private void PersistServer(SavedServerItem item)
    {
        PersistOrReport(() => _serverRepository.Update(SavedServer.Create(
            item.Name, item.Address, item.RequiresSteam, item.RequiresDiscord, item.CfxId,
            item.GameBuild, item.PureMode, item.GameClient)));
    }

    private void DeleteServer(object? parameter)
    {
        var server = parameter as SavedServerItem ?? SelectedServer;

        if (server is null)
        {
            return;
        }

        RequestConfirmation(
            _localizer.Format("ConfirmDeleteServerText", server.Name),
            () => RemoveServer(server));
    }

    private void RemoveServer(SavedServerItem server)
    {
        if (!PersistOrReport(() => _serverRepository.Remove(server.Address)))
        {
            return;
        }

        SavedServers.Remove(server);

        if (ReferenceEquals(SelectedServer, server))
        {
            SelectedServer = null;
        }

        UpdateRowMoveStates();
    }

    private void CreateDesktopShortcut(object? parameter)
    {
        var shortcuts = _shortcuts;

        if (shortcuts is null)
        {
            return;
        }

        var server = parameter as SavedServerItem ?? SelectedServer;

        if (server is null)
        {
            return;
        }

        var desktopPath = shortcuts.DesktopPathProvider();

        if (string.IsNullOrWhiteSpace(desktopPath))
        {
            StatusText = _localizer.Get("StatusCouldNotCreateShortcut");
            return;
        }

        var shortcutPath = Path.Combine(desktopPath, SanitizeFileName(server.Name) + ".lnk");

        if (File.Exists(shortcutPath))
        {
            RequestConfirmation(
                _localizer.Format("ShortcutOverwriteText", Path.GetFileNameWithoutExtension(shortcutPath)),
                () => WriteShortcut(shortcuts, server, shortcutPath));
            return;
        }

        WriteShortcut(shortcuts, server, shortcutPath);
    }

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.Ordinal)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());

        if (sanitized.Length == 0)
        {
            sanitized = "Server";
        }

        if (ReservedDeviceNames.Contains(sanitized.ToUpperInvariant()))
        {
            sanitized += "_";
        }

        return sanitized;
    }

    private void WriteShortcut(ShortcutDependencies shortcuts, SavedServerItem server, string shortcutPath)
    {
        try
        {
            var targetPath = shortcuts.TargetExePathProvider();

            if (targetPath is null)
            {
                StatusText = _localizer.Get("StatusCouldNotCreateShortcut");
                return;
            }

            var iconPath = shortcuts.IconWriter?.TryWrite(server.Icon);
            shortcuts.Creator.CreateShortcut(
                shortcutPath,
                targetPath,
                "--connect \"" + server.Address + "\"",
                iconPath);
            StatusText = _localizer.Get("StatusShortcutCreated");
        }
        catch
        {
            StatusText = _localizer.Get("StatusCouldNotCreateShortcut");
        }
    }

    // The drag entry point: drop position addressed by row, never by a view index —
    // a filtered view and the real list disagree on indices, so the view hands over
    // *which* row to move and the list resolves its position itself.
    public void MoveServerToIndex(SavedServerItem? row, int newIndex)
    {
        if (row is null || !IsReorderAvailable)
        {
            return;
        }

        var currentIndex = SavedServers.IndexOf(row);

        if (currentIndex < 0 || newIndex < 0 || newIndex >= SavedServers.Count || currentIndex == newIndex)
        {
            return;
        }

        // Persist before the visible move: a rejected write then leaves the list
        // untouched rather than showing an order the store does not have.
        if (!PersistOrReport(() => _serverRepository.Move(row.Address, newIndex)))
        {
            return;
        }

        SavedServers.Move(currentIndex, newIndex);
        UpdateRowMoveStates();
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateRowMoveStates()
    {
        for (var i = 0; i < SavedServers.Count; i++)
        {
            SavedServers[i].CanMoveUp = IsReorderAvailable && i > 0;
            SavedServers[i].CanMoveDown = IsReorderAvailable && i < SavedServers.Count - 1;
        }
    }

    private void MoveServerBy(SavedServerItem? row, int offset)
    {
        if (row is not null)
        {
            MoveServerToIndex(row, SavedServers.IndexOf(row) + offset);
        }
    }

    public Task ConnectAsync()
    {
        return RunBusyFlowAsync(ConnectCoreAsync);
    }

    private async Task ConnectCoreAsync()
    {
        StatusText = _localizer.Get("StatusResolving");

        // A pasted address often carries a trailing space or newline; every connect path
        // (typed, forwarded, startup) funnels through here, so trim once at this boundary.
        var address = ServerAddress.Trim();

        try
        {
            var savedServer = _serverRepository.FindByAddress(address)
                ?? FindSavedServerById(address);
            var profile = await _resolver.ResolveAsync(address, savedServer);

            foreach (var app in RequiredApps(profile.Requirements))
            {
                StatusText = _localizer.Format("StatusStartingApp", app);

                if (!await _preparer.TryPrepareAsync(app))
                {
                    StatusText = _localizer.Format("StatusCouldNotStartApp", app);
                    return;
                }
            }

            var result = await _launcher.ConnectAsync(profile);

            StatusText = Describe(result);

            if (result is LaunchResult.Connect or LaunchResult.OpenClient)
            {
                // The launch already happened: a failed save of the reopen memory is
                // reverted silently so the launch status stays on screen.
                var previous = _settings.LastServerAddress;
                _settings.LastServerAddress = address;

                if (!TryPersist(() => _settingsRepository.Save(_settings)))
                {
                    _settings.LastServerAddress = previous;
                }
            }
        }
        catch (InvalidAddressException)
        {
            StatusText = _localizer.Get("StatusInvalidAddress");
        }
    }

    // The busy flows (connect, open client, dev launch) share one shape: busy while they
    // run, and an unexpected fault ends with the last-resort line instead of leaving an
    // in-progress status ("Resolving...") on screen. The startup and forwarded connects
    // call ConnectAsync directly, so this cannot live only in the command.
    private async Task RunBusyFlowAsync(Func<Task> flow)
    {
        IsBusy = true;

        try
        {
            await flow();
        }
        catch (Exception)
        {
            ReportUnexpectedError();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private SavedServer? FindSavedServerById(string address)
    {
        var kind = Domain.ServerAddress.Classify(address);

        return Domain.ServerAddress.IsIdForm(kind)
            ? _serverRepository.FindByCfxId(Domain.ServerAddress.ExtractCfxId(address))
            : null;
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

    public async Task InitializeAsync(string? connectAddress = null)
    {
        try
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

            // Startup refresh: a moved exe re-points its own registration. Silent — the
            // setting stays on and the next start retries.
            if (_settings.WebLinks)
            {
                ApplyWebLinks(true);
            }

            await RefreshCfxStatusAsync();

            var startupAddress = !string.IsNullOrWhiteSpace(connectAddress)
                ? connectAddress
                : _settings.AutoLaunch ? _settings.LastServerAddress : null;

            if (!string.IsNullOrWhiteSpace(startupAddress))
            {
                ServerAddress = startupAddress;
                await ConnectAsync();
            }
        }
        catch (Exception ex) when (IsStoreFault(ex))
        {
            StatusText = _localizer.Get("StatusStartupFailed");
        }

        // The update check runs last and outside the startup guard: an offline or slow
        // GitHub answer must never delay the auto-launch connect, and the check is fully
        // self-swallowing — it also runs when startup itself failed, so the banner can
        // still offer the update that fixes the broken version.
        await CheckForUpdateAsync();
    }

    public async Task OpenClientAsync()
    {
        if (SelectedOpenClient is not { } option)
        {
            return;
        }

        await RunBusyFlowAsync(async () =>
        {
            var result = await _launcher.OpenAsync(option.Client);
            StatusText = Describe(result);
        });
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

    // A locked, read-only or unreadable store surfaces as one of these two; anything else
    // is a bug and stays loud.
    private static bool IsStoreFault(Exception ex) => ex is IOException or UnauthorizedAccessException;

    private static bool TryPersist(Action write)
    {
        try
        {
            write();
            return true;
        }
        catch (Exception ex) when (IsStoreFault(ex))
        {
            return false;
        }
    }

    // Every user-initiated store write goes through here: a failure shows one status line
    // and the caller skips its UI change, so the UI never shows what the store does not hold.
    private bool PersistOrReport(Action write)
    {
        if (TryPersist(write))
        {
            return true;
        }

        StatusText = _localizer.Get("StatusSaveFailed");
        return false;
    }

    private bool TrySaveSettings(Action revert)
    {
        if (PersistOrReport(() => _settingsRepository.Save(_settings)))
        {
            return true;
        }

        revert();
        return false;
    }

    private string Describe(LaunchResult result)
    {
        return result switch
        {
            LaunchResult.Connect => _localizer.Get("StatusLaunchingFiveM"),
            LaunchResult.OpenClient(var client) => _localizer.Format("StatusOpeningClient", InstalledClientOption.DisplayNameOf(client)),
            LaunchResult.NotInstalled(var client) => _localizer.Format("StatusNotInstalled", InstalledClientOption.DisplayNameOf(client)),
            LaunchResult.StartFailed => _localizer.Get("StatusLaunchFailed"),
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