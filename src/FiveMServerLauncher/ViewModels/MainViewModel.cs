using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    private static readonly GameClient[] OpenCandidates = [GameClient.FiveM, GameClient.FiveMEnhanced];

    private readonly ServerResolver _resolver;
    private readonly GameLauncher _launcher;
    private readonly IServerRepository _serverRepository;
    private readonly ExternalAppPreparer _preparer;
    private readonly IClientInstallLocator _installLocator;
    private readonly ConfigurationRepository _settingsRepository;

    private LauncherSettings _settings;

    private string _serverAddress = string.Empty;
    private string _newServerName = string.Empty;
    private string _newServerAddress = string.Empty;
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
        ConfigurationRepository settingsRepository)
    {
        _resolver = resolver;
        _launcher = launcher;
        _serverRepository = serverRepository;
        _preparer = preparer;
        _installLocator = installLocator;
        _settingsRepository = settingsRepository;
        _settings = settingsRepository.Load();
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        AddServerCommand = new RelayCommand(AddServer);
        DeleteServerCommand = new RelayCommand(DeleteServer, CanDeleteServer);
        OpenClientCommand = new AsyncRelayCommand(OpenClientAsync, CanOpenClient);
        SelectOpenClientCommand = new RelayCommand(SelectOpenClient);
        SettingsCommand = new RelayCommand(() => IsSettingsOpen = !IsSettingsOpen);

        SavedServers = new ObservableCollection<SavedServerItem>(
            serverRepository.GetAll().Select(ToItem));
        AvailableOpenClients = new ObservableCollection<InstalledClientOption>();
    }

    public ICommand ConnectCommand { get; }

    public ICommand AddServerCommand { get; }

    public ICommand DeleteServerCommand { get; }

    public ICommand OpenClientCommand { get; }

    public ICommand SelectOpenClientCommand { get; }

    public ICommand SettingsCommand { get; }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    public ObservableCollection<SavedServerItem> SavedServers { get; }

    public ObservableCollection<InstalledClientOption> AvailableOpenClients { get; }

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

    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    public string NewServerName
    {
        get => _newServerName;
        set => SetProperty(ref _newServerName, value);
    }

    public string NewServerAddress
    {
        get => _newServerAddress;
        set => SetProperty(ref _newServerAddress, value);
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
            () => PersistServer(item));

        return item;
    }

    private void PersistServer(SavedServerItem item)
    {
        _serverRepository.Update(SavedServer.Create(item.Name, item.Address, item.RequiresSteam, item.RequiresDiscord));
    }

    private void AddServer()
    {
        SavedServer savedServer;

        try
        {
            savedServer = SavedServer.Create(NewServerName, NewServerAddress);
        }
        catch (ArgumentException)
        {
            StatusText = "Invalid name or address";
            return;
        }

        if (_serverRepository.FindByAddress(savedServer.Address) is not null)
        {
            StatusText = "Server already saved";
            return;
        }

        _serverRepository.Add(savedServer);
        SavedServers.Add(ToItem(savedServer));
        NewServerName = string.Empty;
        NewServerAddress = string.Empty;
        StatusText = "Server saved";
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

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}