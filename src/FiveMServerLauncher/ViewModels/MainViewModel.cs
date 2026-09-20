using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ServerResolver _resolver;
    private readonly GameLauncher _launcher;

    private string _serverAddress = string.Empty;
    private string _statusText = "Listo";
    private bool _isBusy;

    public MainViewModel(ServerResolver resolver, GameLauncher launcher)
    {
        _resolver = resolver;
        _launcher = launcher;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
    }

    public ICommand ConnectCommand { get; }

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
        private set => SetProperty(ref _isBusy, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task ConnectAsync()
    {
        IsBusy = true;
        StatusText = "Resolviendo...";

        try
        {
            var profile = await _resolver.ResolveAsync(ServerAddress);
            var result = await _launcher.ConnectAsync(profile);

            StatusText = result switch
            {
                LaunchResult.Connect => "Lanzando FiveM...",
                LaunchResult.OpenClient(var client) => $"Abriendo {client}...",
                LaunchResult.StartFailed => "No se puede lanzar",
            };
        }
        catch (InvalidAddressException)
        {
            StatusText = "Dirección inválida";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanConnect()
    {
        return !string.IsNullOrWhiteSpace(ServerAddress) && !IsBusy;
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