using System.ComponentModel;
using System.Runtime.CompilerServices;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.ViewModels;

public sealed class CfxStatusItem : INotifyPropertyChanged
{
    private CfxStatus _status;

    public CfxStatusItem(GameClient client)
    {
        Client = client;
        _status = CfxStatus.Unknown;
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
        CfxStatus.Operational => "OPERATIONAL",
        CfxStatus.Degraded => "DEGRADED",
        CfxStatus.PartialOutage => "PARTIAL OUTAGE",
        CfxStatus.MajorOutage => "OUTAGE",
        CfxStatus.Maintenance => "MAINTENANCE",
        _ => "UNKNOWN"
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