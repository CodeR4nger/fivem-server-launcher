using System.Globalization;
using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class PanelVisibilityStateConverterTests
{
    private static Visibility Convert(params object?[] values)
    {
        return (Visibility)new PanelVisibilityStateConverter().Convert(
            values, typeof(Visibility), null!, CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Convert_WhenPanelFlagOpen_ShouldKeepPanelVisible()
    {
        // Given the panel is flagged open
        // When the state is evaluated
        // Then the panel is visible regardless of its fade
        Assert.Equal(Visibility.Visible, Convert(true, 0.0));
    }

    [Fact]
    public void Convert_WhenClosingButStillFadedIn_ShouldKeepPanelMountedForTheMotion()
    {
        // Given the panel requested close but its fade-out is still running
        // When the state is evaluated
        // Then the panel stays visible so the closing motion can render
        Assert.Equal(Visibility.Visible, Convert(false, 1.0));
        Assert.Equal(Visibility.Visible, Convert(false, 0.4));
    }

    [Fact]
    public void Convert_WhenClosedAndFullyFaded_ShouldCollapsePanel()
    {
        // Given the closing motion finished
        // When the state is evaluated
        // Then the panel is collapsed
        Assert.Equal(Visibility.Collapsed, Convert(false, 0.0));
    }

    [Fact]
    public void Convert_WhenInputsAreMalformed_ShouldCollapsePanel()
    {
        // Given missing or mistyped values (e.g. no data context yet)
        // When the state is evaluated
        // Then the panel never lingers visible on bad data
        Assert.Equal(Visibility.Collapsed, Convert());
        Assert.Equal(Visibility.Collapsed, Convert(null, null));
        Assert.Equal(Visibility.Collapsed, Convert("true", "1"));
    }

    [Fact]
    public void Convert_WhenInvertedAndFlagClosed_ShouldKeepDefaultVisibleSurfaceVisible()
    {
        // Given a surface that is visible by default (normal-mode areas, inverted flag)
        // When the flag says "dev mode on" but the fade-out is still running
        // Then the surface stays mounted so the crossfade can render
        Assert.Equal(Visibility.Visible, Convert(false, 1.0));
    }

    [Fact]
    public void Convert_WhenInvertedAndFullyFaded_ShouldCollapseDefaultVisibleSurface()
    {
        // Given the inverted surface's fade-out finished
        // When the state is evaluated
        // Then the surface is collapsed
        Assert.Equal(Visibility.Collapsed, Convert(false, 0.0));
    }

    [Fact]
    public void Convert_WhenInvertedAndInputsAreMalformed_ShouldStayVisibleLikeTheOldFallback()
    {
        // Given no data context yet on a visible-by-default surface
        // When the state is evaluated
        // Then it stays visible (the old binding's FallbackValue behavior)
        Assert.Equal(Visibility.Visible, ConvertWithInvert());
        Assert.Equal(Visibility.Visible, ConvertWithInvert(null, null));
    }

    [Fact]
    public void Convert_WhenFlagOpenButInverted_ShouldCollapseWhileFaded()
    {
        // Given dev mode is on and the normal surface already faded
        // When the state is evaluated
        // Then the surface stays collapsed
        Assert.Equal(Visibility.Collapsed, ConvertWithInvert(true, 0.0));
    }

    private static Visibility ConvertWithInvert(params object?[] values)
    {
        return (Visibility)new PanelVisibilityStateConverter().Convert(
            values, typeof(Visibility), "invert", CultureInfo.InvariantCulture);
    }

    [Fact]
    public void ConvertBack_ShouldThrowBecauseTheBindingIsOneWay()
    {
        // Given the one-way Visibility binding
        // When something tries to convert back
        // Then it fails loudly instead of corrupting state
        Assert.Throws<NotSupportedException>(() =>
            new PanelVisibilityStateConverter().ConvertBack(
                Visibility.Collapsed, new[] { typeof(bool), typeof(double) }, null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Converter_WhenInstantiatedByXaml_ShouldHaveParameterlessCtor()
    {
        // Given XAML instantiates converters through a parameterless ctor
        // When the type is created via Activator
        // Then it succeeds (BAML MissingMethodException regression pin)
        Assert.NotNull(Activator.CreateInstance(typeof(PanelVisibilityStateConverter)));
    }
}
