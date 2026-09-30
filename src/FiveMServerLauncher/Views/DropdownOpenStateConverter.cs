using System.Globalization;
using System.Windows.Data;

namespace FiveMServerLauncher.Views;

// MultiValueConverter feeding Popup.IsOpen in the ComboBox template:
// [ComboBox.IsDropDownOpen, close-hold marker] -> whether the popup window
// should exist. The popup is held open while the collapse animation is still
// running so the closing motion can render; a plain IsDropDownOpen binding
// would hide the popup window before the first frame.
public sealed class DropdownOpenStateConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var open = values.Length > 0 && values[0] is true;
        var holding = values.Length > 1 && values[1] is double opacity && opacity > 0.5;

        return open || holding;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
