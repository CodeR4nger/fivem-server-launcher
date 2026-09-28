using System.ComponentModel;
using System.Runtime.CompilerServices;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Localization;

namespace FiveMServerLauncher.ViewModels;

public sealed class CfxStatusItem : INotifyPropertyChanged
{
    private readonly ILocalizer _localizer;
    private CfxStatus _status;

    public CfxStatusItem(GameClient client, ILocalizer? localizer = null)
    {
        Client = client;
        _localizer = localizer ?? DefaultLocalizer.Get();
        _localizer.LanguageChanged += OnLanguageChanged;
        _status = CfxStatus.Unknown;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLabel)));
    }

    public GameClient Client { get; }

    public string DisplayName => InstalledClientOption.DisplayNameOf(Client);

    public CfxStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusLabel)));
            }
        }
    }

    public string StatusLabel => Status switch
    {
        CfxStatus.Operational => _localizer.Get("CfxStatusOperational"),
        CfxStatus.Degraded => _localizer.Get("CfxStatusDegraded"),
        CfxStatus.PartialOutage => _localizer.Get("CfxStatusPartialOutage"),
        CfxStatus.MajorOutage => _localizer.Get("CfxStatusMajorOutage"),
        CfxStatus.Maintenance => _localizer.Get("CfxStatusMaintenance"),
        _ => _localizer.Get("CfxStatusUnknown")
    };

    public void Apply(CfxStatus status)
    {
        Status = status;
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