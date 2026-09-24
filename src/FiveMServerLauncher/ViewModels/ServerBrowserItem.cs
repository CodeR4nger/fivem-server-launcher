using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.ViewModels;

public sealed class ServerBrowserItem : INotifyPropertyChanged
{
    private const string HiddenEndpointHost = "private-placeholder.cfx.re";
    private static readonly Regex ColorCodePattern = new(@"\^[0-9r]", RegexOptions.Compiled);

    private ServerBrowserItem(string cfxId, string name, GameClient? game, int players, int maxPlayers, string address, string? iconVersion)
    {
        CfxId = cfxId;
        Name = name;
        Game = game;
        Players = players;
        MaxPlayers = maxPlayers;
        Address = address;
        IconVersion = iconVersion;
    }

    public string CfxId { get; }

    public string Name { get; }

    public GameClient? Game { get; }

    public int Players { get; }

    public int MaxPlayers { get; }

    public string Address { get; }

    public string? IconVersion { get; }

    private bool _isSaved;

    public bool IsSaved
    {
        get => _isSaved;
        private set
        {
            if (_isSaved == value)
            {
                return;
            }

            _isSaved = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSaved)));
        }
    }

    public void SetSaved(bool saved)
    {
        IsSaved = saved;
    }

    private Func<Task>? _iconRequest;
    private bool _iconRequested;
    private byte[]? _icon;

    public byte[]? Icon
    {
        get
        {
            if (!_iconRequested && _iconRequest is not null)
            {
                _iconRequested = true;
                _ = _iconRequest();
            }

            return _icon;
        }
    }

    public void SetIconLoader(Func<Task> loader)
    {
        _iconRequest = loader;
    }

    public void SetIcon(byte[] icon)
    {
        _icon = icon;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static ServerBrowserItem FromServer(Master.Server server)
    {
        var data = server.Data;
        var address = ResolveAddress(server);

        return new ServerBrowserItem(
            server.EndPoint,
            StripColorCodes(ResolveName(data)),
            CfxVars.TryGetGameClient(data.Vars),
            data.Clients,
            data.SvMaxclients,
            address,
            CfxVars.TryGetString(data.Vars, "iconVersion"));
    }

    private static string ResolveName(Master.ServerData data)
    {
        return data.Vars.TryGetValue("sv_projectName", out var projectName) && !string.IsNullOrWhiteSpace(projectName)
            ? projectName
            : data.Hostname;
    }

    private static string ResolveAddress(Master.Server server)
    {
        var endpoint = server.Data.ConnectEndPoints.FirstOrDefault();

        return string.IsNullOrEmpty(endpoint)
               || endpoint.Contains(HiddenEndpointHost, StringComparison.OrdinalIgnoreCase)
            ? ServerAddress.FromCfxId(server.EndPoint)
            : endpoint;
    }

    private static string StripColorCodes(string name)
    {
        return ColorCodePattern.Replace(name, string.Empty).Trim();
    }
}
