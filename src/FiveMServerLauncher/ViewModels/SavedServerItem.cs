using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FiveMServerLauncher.ViewModels;

public class SavedServerItem : INotifyPropertyChanged
{
    private readonly Action _changeHandler;

    private bool _requiresSteam;
    private bool _requiresDiscord;

    public SavedServerItem(
        string name,
        string address,
        bool? requiresSteam,
        bool? requiresDiscord,
        Action changeHandler)
    {
        Name = name;
        Address = address;
        _requiresSteam = requiresSteam == true;
        _requiresDiscord = requiresDiscord == true;
        _changeHandler = changeHandler;
    }

    public string Name { get; }

    public string Address { get; }

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