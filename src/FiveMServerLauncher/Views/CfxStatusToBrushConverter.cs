using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Views;

public sealed class CfxStatusToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush OperationalBrush = Freeze(0xFF55C271);
    private static readonly SolidColorBrush DegradedBrush = Freeze(0xFFF5A623);
    private static readonly SolidColorBrush OutageBrush = Freeze(0xFFE05252);
    private static readonly SolidColorBrush UnknownBrush = Freeze(0xFF3A3A3A);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            CfxStatus.Operational => OperationalBrush,
            CfxStatus.Degraded => DegradedBrush,
            CfxStatus.PartialOutage => OutageBrush,
            CfxStatus.MajorOutage => OutageBrush,
            _ => UnknownBrush,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static SolidColorBrush Freeze(uint argb)
    {
        var brush = new SolidColorBrush(Color.FromArgb(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb));
        brush.Freeze();
        return brush;
    }
}