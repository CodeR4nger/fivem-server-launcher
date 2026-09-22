using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Views;

public sealed class GameClientToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush FiveMBrush = Freeze(0xFFF5A623);
    private static readonly SolidColorBrush FiveMEnhancedBrush = Freeze(0xFF4DA3FF);
    private static readonly SolidColorBrush RedMBrush = Freeze(0xFFE05252);
    private static readonly SolidColorBrush UnresolvedBrush = Freeze(0xFF3A3A3A);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            GameClient.FiveM => FiveMBrush,
            GameClient.FiveMEnhanced => FiveMEnhancedBrush,
            GameClient.RedM => RedMBrush,
            _ => UnresolvedBrush,
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