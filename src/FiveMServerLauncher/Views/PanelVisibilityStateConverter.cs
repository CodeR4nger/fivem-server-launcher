using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FiveMServerLauncher.Views;

// MultiValueConverter feeding the Visibility of the animated panels
// (settings, the Dev Mode areas, the dialog's LOCAL DEV side panel):
// [panel-open flag, the panel's own Opacity] -> Visibility. The panel's fade
// doubles as its close-hold state: while the closing motion is still running
// the opacity is above zero, so the panel stays mounted and the motion can
// render; when the fade completes the panel collapses on its own. A plain
// flag-to-Visibility binding would collapse it before the first frame.
public sealed class PanelVisibilityStateConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // "invert" flips the flag for the surfaces that are visible by default
        // (the normal-mode status block and split button live under an inverse
        // dev-mode flag) — including the malformed-input fallback, which must
        // stay visible for them just like their old FallbackValue did.
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);
        var open = values.Length > 0 && values[0] is true;

        if (invert)
        {
            open = !open;
        }

        var holding = values.Length > 1 && values[1] is double opacity && opacity > 0;

        return open || holding ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
