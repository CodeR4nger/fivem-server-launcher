using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Localization;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.ViewModels;

public class SavedServerItem : INotifyPropertyChanged
{
    private readonly Action _changeHandler;
    private readonly ILocalizer _localizer;

    private bool _requiresSteam;
    private bool _requiresDiscord;
    private bool _online;
    private int? _players;
    private int? _maxPlayers;
    private byte[]? _icon;
    private GameClient? _game;
    private bool? _publishedSteamRequired;
    private int? _publishedGameBuild;
    private int? _publishedPureMode;
    private GameBuildData? _gameBuildData;

    public SavedServerItem(
        string name,
        string address,
        bool? requiresSteam,
        bool? requiresDiscord,
        Action changeHandler,
        ILocalizer localizer,
        string? cfxId = null,
        int? gameBuild = null,
        int? pureMode = null,
        GameClient? gameClient = null,
        GameBuildData? gameBuildData = null)
    {
        Name = name;
        Address = address;
        CfxId = cfxId;
        GameBuild = gameBuild;
        PureMode = pureMode;
        GameClient = gameClient;
        _requiresSteam = requiresSteam == true;
        _requiresDiscord = requiresDiscord == true;
        _changeHandler = changeHandler;
        _localizer = localizer;
        _gameBuildData = gameBuildData;
        // Rows are short-lived (deleted/edited); a strong subscription to the
        // app-lifetime localizer would root every row forever. The weak event
        // manager holds the row only while something else does.
        WeakEventManager<ILocalizer, EventArgs>.AddHandler(
            _localizer, nameof(ILocalizer.LanguageChanged), OnLanguageChanged);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Raise(nameof(StatusLabel));
        RaiseDetailChanged();
    }

    public string Name { get; }

    private const int UnselectedNameLimit = 30;
    private const int SelectedNameLimit = 20;

    public string TrimmedName => Trim(Name, _isSelected ? SelectedNameLimit : UnselectedNameLimit);

    private bool _canMoveUp;
    private bool _canMoveDown;

    // Driven by the view model on every structural change: the row itself never
    // knows its position. Both are false while a search filter is active.
    public bool CanMoveUp
    {
        get => _canMoveUp;
        set => SetProperty(ref _canMoveUp, value);
    }

    public bool CanMoveDown
    {
        get => _canMoveDown;
        set => SetProperty(ref _canMoveDown, value);
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrimmedName)));
        }
    }

    private static string Trim(string name, int limit)
    {
        return name.Length <= limit ? name : string.Concat(name.AsSpan(0, limit - 1), "…");
    }

    public string Address { get; }

    public string? CfxId { get; private set; }

    public int? GameBuild { get; }

    public int? PureMode { get; }

    public GameClient? GameClient { get; }

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
        RaiseDetailChanged();
    }

    public bool RequiresSteam
    {
        get => _requiresSteam;
        set
        {
            if (SetProperty(ref _requiresSteam, value))
            {
                Raise(nameof(EffectiveRequiresSteam));
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
                Raise(nameof(EffectiveRequiresDiscord));
                _changeHandler();
            }
        }
    }

    // What a connect would require: the manual flags merged with the published
    // Steam flag by the connect rule. A stale published flag from a server that
    // has gone offline is never applied.
    public bool EffectiveRequiresSteam =>
        ServerRequirements.SteamRequiredFor(Online ? _publishedSteamRequired : null, RequiresSteam) == true;

    public bool EffectiveRequiresDiscord => RequiresDiscord;

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
            ? _localizer.Get("StatusUnresolved")
            : Online && Players is { } players && MaxPlayers is { } max
                ? $"{players}/{max}"
                : _localizer.Get("StatusOffline");

    // ---- Detail pane data ----

    // Mirrors the connect rule for loopback servers: a listed manual cfx id wins
    // (its published values apply); otherwise the manual values do.
    private bool HasManualParameters =>
        ServerAddress.IsLoopbackAddress(Address)
        && (GameBuild.HasValue || PureMode.HasValue || GameClient.HasValue);

    public ServerDetailState DetailState =>
        Online ? ServerDetailState.Listed
        : HasManualParameters ? ServerDetailState.Manual
        : HasCfxId ? ServerDetailState.Offline
        : ServerDetailState.NotListed;

    public bool ShowsDetails => DetailState is ServerDetailState.Listed or ServerDetailState.Manual;

    public string DetailStateLabel => DetailState switch
    {
        ServerDetailState.Offline => _localizer.Get("DetailStateOffline"),
        ServerDetailState.NotListed => _localizer.Get("DetailStateNotListed"),
        ServerDetailState.Manual => _localizer.Get("DetailStateManual"),
        _ => string.Empty
    };

    private bool UsesManualValues => DetailState == ServerDetailState.Manual;

    private int? DetailGameBuild => UsesManualValues ? GameBuild : _publishedGameBuild;

    private int? DetailPureMode => UsesManualValues ? PureMode : _publishedPureMode;

    public GameClient? DetailGame => UsesManualValues ? GameClient : Game;

    public string DetailGameLabel => LabelOf(DetailGame);

    public string PlayersText =>
        Online && Players is { } players && MaxPlayers is { } max ? $"{players}/{max}" : string.Empty;

    public string BuildLabel => DetailGameBuild is { } build
        ? GameBuilds.NameOf(build, _gameBuildData) is { } name ? $"{build} · {name}" : build.ToString()
        : _localizer.Get("DetailBuildServerDefault");

    public string PureModeLabel => DetailPureMode is { } pure
        ? pure.ToString()
        : _localizer.Get("DetailPureModeOff");

    public void SetGameBuildData(GameBuildData data)
    {
        _gameBuildData = data;
        Raise(nameof(BuildLabel));
    }

    private void RaiseDetailChanged()
    {
        Raise(nameof(DetailState));
        Raise(nameof(ShowsDetails));
        Raise(nameof(DetailStateLabel));
        Raise(nameof(PlayersText));
        Raise(nameof(BuildLabel));
        Raise(nameof(PureModeLabel));
        Raise(nameof(DetailGame));
        Raise(nameof(DetailGameLabel));
        Raise(nameof(EffectiveRequiresSteam));
        Raise(nameof(EffectiveRequiresDiscord));
    }

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

    public string GameTagLabel => LabelOf(Game);

    private static string LabelOf(GameClient? game) =>
        game is { } known ? InstalledClientOption.DisplayNameOf(known) : string.Empty;

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
            _publishedSteamRequired = presence.SteamRequired;
            _publishedGameBuild = presence.GameBuild;
            _publishedPureMode = presence.PureMode;
        }
        else
        {
            // Last known build/pure/steam are kept (like the game) but the detail
            // state hides them behind Offline; nothing stale is ever applied.
            Online = false;
            Players = null;
            MaxPlayers = null;
        }

        RaiseDetailChanged();
    }

    public void SetIcon(byte[] icon)
    {
        Icon = icon;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Raise(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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