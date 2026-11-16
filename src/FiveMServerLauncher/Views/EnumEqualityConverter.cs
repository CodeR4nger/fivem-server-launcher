using System.Globalization;
using System.Windows.Data;

namespace FiveMServerLauncher.Views;

// An enum value ↔ a radio chip's IsChecked: checked when the value equals the chip's
// ConverterParameter; checking writes the parameter back, unchecking writes nothing (the
// newly checked chip owns the write). XAML-instantiated: true parameterless ctor.
public sealed class EnumEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Equals(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? parameter : Binding.DoNothing;
}
