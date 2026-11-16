using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Views;

// Saved server detail state → the Servers page's status dot, in the CFX status dots'
// palette: listed green, manual (loopback values) amber, and offline, not listed or nothing
// selected grey — red already means RedM and errors. XAML-instantiated: true parameterless ctor.
public sealed class ServerDetailStateToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ListedBrush = Freeze(0x3F, 0xB9, 0x50);
    private static readonly SolidColorBrush ManualBrush = Freeze(0xD2, 0x99, 0x22);
    private static readonly SolidColorBrush InactiveBrush = Freeze(0x7A, 0x7A, 0x7A);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        ServerDetailState.Listed => ListedBrush,
        ServerDetailState.Manual => ManualBrush,
        _ => InactiveBrush
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
