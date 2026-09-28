using System.Windows.Data;

namespace FiveMServerLauncher.Views;

public class LocExtension : Binding
{
    public LocExtension(string key)
        : base(key)
    {
        Source = LocalizationSource.Instance;
        Mode = BindingMode.OneWay;
    }
}
