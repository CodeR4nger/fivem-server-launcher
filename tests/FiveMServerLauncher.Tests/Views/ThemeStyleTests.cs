using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

// The theme's reusable styles: they apply to their target type, and every motion trigger
// honours the inherited Motion.IsEnabled switch (off → end state, no running animation).
public class ThemeStyleTests
{
    public static TheoryData<string, Type> ReusableStyles => new()
    {
        { "PrimaryButtonStyle", typeof(Button) },
        { "SecondaryButtonStyle", typeof(Button) },
        { "IconButtonStyle", typeof(Button) },
        { "FilterChipStyle", typeof(ToggleButton) },
        { "FilterChipStyle", typeof(RadioButton) },
        { "ToggleSwitchStyle", typeof(ToggleButton) },
        { "SegmentedControlStyle", typeof(ListBox) },
        { "TextInputStyle", typeof(TextBox) },
        { "PillTextInputStyle", typeof(TextBox) },
        { "ServerMarkStyle", typeof(ContentControl) },
        { "SettingsRowStyle", typeof(HeaderedContentControl) },
        { "PopupMenuStyle", typeof(ContextMenu) },
        { "PopupMenuItemStyle", typeof(MenuItem) },
        { "ContentPanelStyle", typeof(Border) },
        { "CardStyle", typeof(Border) },
        { "SettingsGroupStyle", typeof(Border) },
        { "ModalFrameStyle", typeof(Border) },
        { "GameTagStyle", typeof(Border) },
        { "FadeSurfaceStyle", typeof(StackPanel) },
        { "PageSurfaceStyle", typeof(UserControl) },
        { "NavTabStyle", typeof(Button) },
        { "PageTitleStyle", typeof(TextBlock) },
        { "SectionHeaderStyle", typeof(TextBlock) },
        { "RowTitleStyle", typeof(TextBlock) },
        { "BodyTextStyle", typeof(TextBlock) },
        { "CaptionTextStyle", typeof(TextBlock) },
        { "DangerTextStyle", typeof(TextBlock) },
        { "EmptyStateTextStyle", typeof(TextBlock) },
        { "GameTagTextStyle", typeof(TextBlock) },
        { "PlaceholderTextStyle", typeof(TextBlock) },
        { "ChipCountStyle", typeof(TextBlock) },
        { "ChipDotStyle", typeof(System.Windows.Shapes.Ellipse) },
    };

    [Theory]
    [MemberData(nameof(ReusableStyles))]
    public void ReusableStyle_ShouldApplyToItsTargetType(string key, Type target)
    {
        // Given a fresh element of the target type
        var applied = StaThread.Run(() =>
        {
            var element = (FrameworkElement)Activator.CreateInstance(target)!;

            // When the theme style is set and the template applied
            element.Style = (Style)new ThemeResources()[key];
            element.ApplyTemplate();
            return element.Style is not null;
        });

        // Then it applies without throwing
        Assert.True(applied);
    }

    [Fact]
    public void ToggleSwitch_WithMotionOff_ShouldSnapTheKnobWithoutAnimating()
    {
        // Given a switch with motion off
        var (x, animated) = StaThread.Run(() =>
        {
            var toggle = Switch(motion: false);

            // When it is switched on
            toggle.IsChecked = true;

            var offset = KnobOffset(toggle);
            return (offset.X, offset.HasAnimatedProperties);
        });

        // Then the knob sits at the on position and no storyboard runs
        Assert.Equal(26, x);
        Assert.False(animated);
    }

    [Fact]
    public void ToggleSwitch_WithMotionOn_ShouldAnimateTheKnob()
    {
        // Given a switch with motion on
        var animated = StaThread.Run(() =>
        {
            var toggle = Switch(motion: true);

            // When it is switched on
            toggle.IsChecked = true;

            return KnobOffset(toggle).HasAnimatedProperties;
        });

        // Then the knob slides (a storyboard drives it)
        Assert.True(animated);
    }

    [Fact]
    public void ToggleSwitch_WhenMotionTurnsOffWhileOn_ShouldDropTheAnimationAndStayOn()
    {
        // Given a switch that slid on with motion
        var (x, animated) = StaThread.Run(() =>
        {
            var toggle = Switch(motion: true);
            using var host = new OffscreenHost(toggle);
            toggle.IsChecked = true;
            StaThread.Pump();

            // When motion is switched off
            Motion.SetIsEnabled(toggle, false);
            StaThread.Pump(300);

            var offset = KnobOffset(toggle);
            return (offset.X, offset.HasAnimatedProperties);
        });

        // Then the knob holds the on position from the setter, not a held animation
        Assert.Equal(26, x);
        Assert.False(animated);
    }

    [Fact]
    public void FadeSurface_WithMotionOff_ShouldSnapVisibleAndHiddenWithoutAnimating()
    {
        // Given a fading surface with motion off
        var (shownOpacity, hiddenOpacity, animated) = StaThread.Run(() =>
        {
            var surface = FadeSurface(motion: false);

            // When it is shown, then hidden
            Motion.SetShown(surface, true);
            var shown = surface.Opacity;
            var animatedWhileShown = surface.HasAnimatedProperties;
            Motion.SetShown(surface, false);

            return (shown, surface.Opacity, animatedWhileShown || surface.HasAnimatedProperties);
        });

        // Then it snaps between the end states and never animates
        Assert.Equal(1, shownOpacity);
        Assert.Equal(0, hiddenOpacity);
        Assert.False(animated);
    }

    [Fact]
    public void FadeSurface_WithMotionOn_ShouldFade()
    {
        // Given a fading surface with motion on
        var animated = StaThread.Run(() =>
        {
            var surface = FadeSurface(motion: true);

            // When it is shown
            Motion.SetShown(surface, true);

            return surface.HasAnimatedProperties;
        });

        // Then a storyboard drives its opacity
        Assert.True(animated);
    }

    [Fact]
    public void FadeSurface_WhenMotionTurnsOffWhileShown_ShouldStayVisible()
    {
        // Given a surface faded in with motion
        var (opacity, animated) = StaThread.Run(() =>
        {
            var surface = FadeSurface(motion: true);
            using var host = new OffscreenHost(surface);
            Motion.SetShown(surface, true);
            StaThread.Pump();

            // When motion is switched off (the removal applies on the next clock tick of
            // the rendering element)
            Motion.SetIsEnabled(surface, false);
            StaThread.Pump(300);

            return (surface.Opacity, surface.HasAnimatedProperties);
        });

        // Then it is fully visible from the setter, with no held animation left
        Assert.Equal(1, opacity);
        Assert.False(animated);
    }

    [Fact]
    public void FadeSurface_WhenMotionIsOnBeforeItIsShown_ShouldEndFullyVisibleThenHidden()
    {
        // Given a rendering surface whose motion switch resolves before its shown flag
        // (the window's binding can land first): regression for layered in/out triggers
        var (shown, hidden) = StaThread.Run(() =>
        {
            var surface = FadeSurface(motion: true);
            using var host = new OffscreenHost(surface);
            StaThread.Pump();

            // When it is shown and the fade completes, then hidden
            Motion.SetShown(surface, true);
            StaThread.Pump(500);
            var visible = surface.Opacity;
            Motion.SetShown(surface, false);
            StaThread.Pump(500);

            return (visible, surface.Opacity);
        });

        // Then it ends fully visible, then fully hidden
        Assert.Equal(1, shown);
        Assert.Equal(0, hidden);
    }

    [Fact]
    public void ToggleSwitch_WithMotionOn_ShouldSlideBackWhenSwitchedOff()
    {
        // Given a rendering switch that slid on
        var x = StaThread.Run(() =>
        {
            var toggle = Switch(motion: true);
            using var host = new OffscreenHost(toggle);
            toggle.IsChecked = true;
            StaThread.Pump(400);

            // When it is switched off and the slide completes
            toggle.IsChecked = false;
            StaThread.Pump(400);

            return KnobOffset(toggle).X;
        });

        // Then the knob is back at the off position
        Assert.Equal(4, x);
    }

    [Fact]
    public void PillButton_WithMotionOff_ShouldNotAnimateTheHoverLayer()
    {
        // Given a primary button with motion off, its template applied
        var animated = StaThread.Run(() =>
        {
            var button = new Button { Style = (Style)new ThemeResources()["PrimaryButtonStyle"] };
            Motion.SetIsEnabled(button, false);
            button.ApplyTemplate();
            var layer = (Border)button.Template.FindName("HoverLayer", button);

            // When it is pressed (IsPressed is the template's press input)
            typeof(ButtonBase).GetMethod("SetIsPressed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(button, [true]);

            return (layer.HasAnimatedProperties, layer.Opacity);
        });

        // Then the layer snaps to the press opacity
        Assert.False(animated.HasAnimatedProperties);
        Assert.Equal(0.16, animated.Opacity, 3);
    }

    [Fact]
    public void PageSurface_WithMotionOff_ShouldSnapShownAndHiddenWithoutAnimating()
    {
        // Given a page with motion off
        var (hiddenAtStart, shown, opacity, hidden, animated) = StaThread.Run(() =>
        {
            var page = PageSurface(motion: false);
            var startHidden = page.Visibility == Visibility.Collapsed;

            // When it is shown, then hidden
            Motion.SetShown(page, true);
            var visible = page.Visibility == Visibility.Visible;
            var shownOpacity = page.Opacity;
            var animatedWhileShown = page.HasAnimatedProperties;
            Motion.SetShown(page, false);

            return (startHidden, visible, shownOpacity, page.Visibility == Visibility.Collapsed,
                animatedWhileShown || page.HasAnimatedProperties);
        });

        // Then it snaps between collapsed and fully visible and never animates
        Assert.True(hiddenAtStart);
        Assert.True(shown);
        Assert.Equal(1, opacity);
        Assert.True(hidden);
        Assert.False(animated);
    }

    [Fact]
    public void PageSurface_WithMotionOn_ShouldFadeAndSlideIn()
    {
        // Given a page with motion on
        var (animated, rising) = StaThread.Run(() =>
        {
            var page = PageSurface(motion: true);

            // When it is shown
            Motion.SetShown(page, true);

            var offset = (TranslateTransform)page.RenderTransform;
            return (page.HasAnimatedProperties, offset.HasAnimatedProperties);
        });

        // Then storyboards drive its opacity and its rise
        Assert.True(animated);
        Assert.True(rising);
    }

    [Fact]
    public void PageSurface_WhenShownAgainWithMotion_ShouldEndFullyVisibleInPlace()
    {
        // Given a rendering page shown, hidden and shown again (a return visit)
        var (opacity, offset) = StaThread.Run(() =>
        {
            var page = PageSurface(motion: true);
            using var host = new OffscreenHost(page);
            StaThread.Pump();
            Motion.SetShown(page, true);
            StaThread.Pump(400);
            Motion.SetShown(page, false);
            StaThread.Pump();

            // When it is shown again and the motion completes
            Motion.SetShown(page, true);
            StaThread.Pump(400);

            return (page.Opacity, ((TranslateTransform)page.RenderTransform).Y);
        });

        // Then it rests fully visible at its place
        Assert.Equal(1, opacity);
        Assert.Equal(0, offset);
    }

    [Fact]
    public void PageSurface_WhenMotionTurnsOffWhileShown_ShouldDropTheAnimation()
    {
        // Given a page shown with motion
        var (opacity, animated) = StaThread.Run(() =>
        {
            var page = PageSurface(motion: true);
            using var host = new OffscreenHost(page);
            Motion.SetShown(page, true);
            StaThread.Pump();

            // When motion is switched off
            Motion.SetIsEnabled(page, false);
            StaThread.Pump(300);

            return (page.Opacity, page.HasAnimatedProperties);
        });

        // Then it is fully visible with no held animation
        Assert.Equal(1, opacity);
        Assert.False(animated);
    }

    // Clock control operations (RemoveStoryboard) are applied by the ticking clock of an
    // element that renders, so the motion-switch tests host it in an offscreen window.
    private sealed class OffscreenHost : IDisposable
    {
        private readonly Window _window;

        public OffscreenHost(FrameworkElement content)
        {
            _window = new Window
            {
                Content = content,
                Left = -10000,
                Top = -10000,
                Width = 200,
                Height = 100,
                ShowInTaskbar = false,
                ShowActivated = false
            };
            _window.Show();
        }

        public void Dispose() => _window.Close();
    }

    private static ToggleButton Switch(bool motion)
    {
        var toggle = new ToggleButton { Style = (Style)new ThemeResources()["ToggleSwitchStyle"] };
        Motion.SetIsEnabled(toggle, motion);
        toggle.ApplyTemplate();
        return toggle;
    }

    private static TranslateTransform KnobOffset(ToggleButton toggle) =>
        (TranslateTransform)((UIElement)toggle.Template.FindName("Knob", toggle)).RenderTransform;

    private static Border FadeSurface(bool motion)
    {
        var surface = new Border { Style = (Style)new ThemeResources()["FadeSurfaceStyle"] };
        Motion.SetIsEnabled(surface, motion);
        return surface;
    }

    private static UserControl PageSurface(bool motion)
    {
        var page = new UserControl { Style = (Style)new ThemeResources()["PageSurfaceStyle"] };
        Motion.SetIsEnabled(page, motion);
        return page;
    }
}
