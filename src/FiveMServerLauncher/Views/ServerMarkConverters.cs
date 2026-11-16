using System.Globalization;
using System.Windows.Data;

namespace FiveMServerLauncher.Views;

// Server name → mark initials (ServerMark.Initials). XAML-instantiated: true parameterless ctor.
public sealed class ServerMarkInitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ServerMark.Initials(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Server name → frozen mark background brush (ServerMark.Color). XAML-instantiated: true
// parameterless ctor.
public sealed class ServerMarkBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ServerMark.Brush(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

// Mark height → initials font size (ServerMark.InitialsFontSize). XAML-instantiated: true
// parameterless ctor.
public sealed class ServerMarkFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        ServerMark.InitialsFontSize(value is double size ? size : 0);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
