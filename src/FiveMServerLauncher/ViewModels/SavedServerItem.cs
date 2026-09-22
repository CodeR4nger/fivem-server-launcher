using System.ComponentModel;
using System.Runtime.CompilerServices;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.ViewModels;

public class SavedServerItem : INotifyPropertyChanged
{
    private readonly Action _changeHandler;

    private bool _requiresSteam;
    private bool _requiresDiscord;
    private bool _online;
    private int? _players;
    private int? _maxPlayers;
    private byte[]? _icon;
    private GameClient? _game;

    public SavedServerItem(
        string name,
        string address,
        bool? requiresSteam,
        bool? requiresDiscord,
        Action changeHandler,
        string? cfxId = null)
    {
        Name = name;
        Address = address;
        CfxId = cfxId;
        _requiresSteam = requiresSteam == true;
        _requiresDiscord = requiresDiscord == true;
        _changeHandler = changeHandler;
    }

    public string Name { get; }

    public string Address { get; }

    public string? CfxId { get; private set; }

    public bool HasCfxId => CfxId is not null;

    public void SetCfxId(string cfxId)
    {
        if (CfxId == cfxId)
        {
            return;
        }

        CfxId = cfxId;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CfxId)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasCfxId)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLabel)));
    }

    public bool RequiresSteam
    {
        get => _requiresSteam;
        set
        {
            if (SetProperty(ref _requiresSteam, value))
            {
                _changeHandler();
            }
        }
    }

    public bool RequiresDiscord
    {
        get => _requiresDiscord;
        set
        {
            if (SetProperty(ref _requiresDiscord, value))
            {
                _changeHandler();
            }
        }
    }

    public bool Online
    {
        get => _online;
        private set
        {
            if (SetProperty(ref _online, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLabel)));
            }
        }
    }

    public int? Players
    {
        get => _players;
        private set
        {
            if (SetProperty(ref _players, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLabel)));
            }
        }
    }

    public int? MaxPlayers
    {
        get => _maxPlayers;
        private set
        {
            if (SetProperty(ref _maxPlayers, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLabel)));
            }
        }
    }

    public string StatusLabel =>
        !HasCfxId
            ? "UNRESOLVED"
            : Online && Players is { } players && MaxPlayers is { } max
                ? $"{players}/{max}"
                : "OFFLINE";

    public GameClient? Game
    {
        get => _game;
        private set
        {
            if (SetProperty(ref _game, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameTagLabel)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasGame)));
            }
        }
    }

    public string GameTagLabel => Game is { } game ? InstalledClientOption.DisplayNameOf(game) : string.Empty;

    public bool HasGame => Game is not null;

    public byte[]? Icon
    {
        get => _icon;
        private set
        {
            if (SetProperty(ref _icon, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasIcon)));
            }
        }
    }

    public bool HasIcon => Icon is not null;

    public void ApplyPresence(ServerPresence presence)
    {
        if (presence.Game is { } game)
        {
            Game = game;
        }

        if (presence.Online)
        {
            Online = true;
            Players = presence.Players;
            MaxPlayers = presence.MaxPlayers;
        }
        else
        {
            Online = false;
            Players = null;
            MaxPlayers = null;
        }
    }

    public void SetIcon(byte[] icon)
    {
        Icon = icon;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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