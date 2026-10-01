using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FiveMServerLauncher.Views;

// MultiValueConverter feeding the Visibility of the update banner:
// [banner state flags] -> Visibility. The banner mounts as soon as any of its
// flags is set (available / in-progress / failed) and collapses when none is.
// Callers pick which flags they bind: the banner binds all three, the dismiss
// button binds the two states it is honest from.
public sealed class AnyTrueToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Any(v => v is true) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
