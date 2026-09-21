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
    private readonly ServerResolver _resolver;
    private readonly GameLauncher _launcher;
    private readonly IServerRepository _serverRepository;
    private readonly IRequirementReadiness _readiness;

    private string _serverAddress = string.Empty;
    private string _newServerName = string.Empty;
    private string _newServerAddress = string.Empty;
    private string _statusText = "Ready";
    private bool _isBusy;
    private SavedServerItem? _selectedServer;

    public MainViewModel(
        ServerResolver resolver,
        GameLauncher launcher,
        IServerRepository serverRepository,
        IRequirementReadiness readiness)
    {
        _resolver = resolver;
        _launcher = launcher;
        _serverRepository = serverRepository;
        _readiness = readiness;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        AddServerCommand = new RelayCommand(AddServer);
        DeleteServerCommand = new RelayCommand(DeleteServer, CanDeleteServer);

        SavedServers = new ObservableCollection<SavedServerItem>(
            serverRepository.GetAll().Select(ToItem));
    }

    public ICommand ConnectCommand { get; }

    public ICommand AddServerCommand { get; }

    public ICommand DeleteServerCommand { get; }

    public ObservableCollection<SavedServerItem> SavedServers { get; }

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
        private set => SetProperty(ref _isBusy, value);
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

            var missingRequirements = await FindMissingRequirementsAsync(profile.Requirements);

            if (missingRequirements.Count > 0)
            {
                StatusText = FormatMissingRequirements(missingRequirements);
                return;
            }

            var result = await _launcher.ConnectAsync(profile);

            StatusText = result switch
            {
                LaunchResult.Connect => "Launching FiveM...",
                LaunchResult.OpenClient(var client) => $"Opening {client}...",
                LaunchResult.StartFailed => "Launch failed",
                _ => throw new InvalidOperationException("Unknown LaunchResult"),
            };
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

    private async Task<List<ExternalApp>> FindMissingRequirementsAsync(ServerRequirements requirements)
    {
        var requiredApps = new List<ExternalApp>();

        if (requirements.SteamRequired == true)
        {
            requiredApps.Add(ExternalApp.Steam);
        }

        if (requirements.DiscordRequired == true)
        {
            requiredApps.Add(ExternalApp.Discord);
        }

        var missing = new List<ExternalApp>();

        foreach (var app in requiredApps)
        {
            if (!await _readiness.IsRunningAsync(app))
            {
                missing.Add(app);
            }
        }

        return missing;
    }

    private static string FormatMissingRequirements(List<ExternalApp> missingRequirements)
    {
        return string.Join(" / ", missingRequirements.Select(app => $"Requires {app} (not running)"));
    }

    private bool CanConnect()
    {
        return !string.IsNullOrWhiteSpace(ServerAddress) && !IsBusy;
    }

    private bool CanDeleteServer()
    {
        return SelectedServer is not null;
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