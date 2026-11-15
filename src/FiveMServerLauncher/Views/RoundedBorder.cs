using System.Windows;
using System.Windows.Controls;

namespace FiveMServerLauncher.Views;

// A Border whose corner radius is clamped to half its rendered short side, so "fully
// rounded" (RadiusPill, 999) draws a true pill at any size — a plain Border draws an
// over-large radius as an ellipse. Used by the theme's pill templates. The clamp is a
// coercion re-run whenever the arranged size changes, so the requested value is kept and followed.
// XAML-instantiated: keep the true parameterless ctor.
public class RoundedBorder : Border
{
    static RoundedBorder()
    {
        CornerRadiusProperty.OverrideMetadata(
            typeof(RoundedBorder),
            new FrameworkPropertyMetadata(
                default(CornerRadius),
                FrameworkPropertyMetadataOptions.AffectsRender,
                propertyChangedCallback: null,
                coerceValueCallback: ClampToSize));
    }

    private Size _arranged;

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arranged = base.ArrangeOverride(finalSize);

        if (arranged != _arranged)
        {
            _arranged = arranged;
            CoerceValue(CornerRadiusProperty);
        }

        return arranged;
    }

    private static object ClampToSize(DependencyObject element, object value)
    {
        var requested = (CornerRadius)value;
        var size = ((RoundedBorder)element)._arranged;

        if (size.Width <= 0 || size.Height <= 0)
        {
            return requested;
        }

        var max = Math.Min(size.Width, size.Height) / 2;
        return new CornerRadius(
            Math.Min(requested.TopLeft, max),
            Math.Min(requested.TopRight, max),
            Math.Min(requested.BottomRight, max),
            Math.Min(requested.BottomLeft, max));
    }
}
