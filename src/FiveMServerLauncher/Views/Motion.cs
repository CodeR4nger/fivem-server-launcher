using System.Windows;

namespace FiveMServerLauncher.Views;

// The interface-motion switch. IsEnabled is INHERITED and OFF by default: MainWindow binds it
// once at its root to MainViewModel.IsMotionEnabled and every element below reads the same
// value. Off by default so nothing animates before that binding resolves (controls are parsed
// before the window has its DataContext) — with motion off no storyboard ever starts. The theme's
// motion triggers require it (with it off no storyboard starts and surfaces snap to their end
// state) and code-behind motion checks GetIsEnabled before animating. Popups that do not
// inherit from the window (ContextMenu) copy it from their PlacementTarget in their style.
// Shown is the per-surface "should be visible" input of the shared fade style
// (Theme.xaml FadeSurfaceStyle): bind it to the view-model flag that opens the surface.
public static class Motion
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(Motion),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty ShownProperty = DependencyProperty.RegisterAttached(
        "Shown", typeof(bool), typeof(Motion), new PropertyMetadata(false));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetShown(DependencyObject element) => (bool)element.GetValue(ShownProperty);

    public static void SetShown(DependencyObject element, bool value) => element.SetValue(ShownProperty, value);
}
