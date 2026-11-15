using System.Globalization;
using System.Windows.Data;

namespace FiveMServerLauncher.Views;

// bool → !bool (a non-bool reads as false, so it converts to true). Feeds Motion.Shown on
// surfaces that show while a flag is OFF. XAML-instantiated: true parameterless ctor.
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
