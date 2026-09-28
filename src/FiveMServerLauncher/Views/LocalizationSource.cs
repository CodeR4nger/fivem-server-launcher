using System.ComponentModel;
using FiveMServerLauncher.Localization;

namespace FiveMServerLauncher.Views;

public sealed class LocalizationSource : INotifyPropertyChanged
{
    public static readonly LocalizationSource Instance = new();

    private ILocalizer? _localizer;

    private LocalizationSource()
    {
    }

    public void Attach(ILocalizer localizer)
    {
        _localizer = localizer;
        localizer.LanguageChanged += (_, _) => RaiseIndexerChanged();
        RaiseIndexerChanged();
    }

    public string this[string key] => _localizer?.Get(key) ?? key;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void RaiseIndexerChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
