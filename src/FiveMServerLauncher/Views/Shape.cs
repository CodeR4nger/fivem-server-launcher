using System.Windows;

namespace FiveMServerLauncher.Views;

// Corner radius as a style-settable input to the shared templates (Button/TextBox have no
// CornerRadius of their own): PillButtonTemplate and TextInputStyle draw with it, so one
// template serves the pill, icon, menu-row and 12-radius input variants.
public static class Shape
{
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
        "CornerRadius", typeof(CornerRadius), typeof(Shape), new PropertyMetadata(default(CornerRadius)));

    public static CornerRadius GetCornerRadius(DependencyObject element) =>
        (CornerRadius)element.GetValue(CornerRadiusProperty);

    public static void SetCornerRadius(DependencyObject element, CornerRadius value) =>
        element.SetValue(CornerRadiusProperty, value);
}
