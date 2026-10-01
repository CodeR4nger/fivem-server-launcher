using System.Globalization;
using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class AnyTrueToVisibilityConverterTests
{
    private static Visibility Convert(params object?[] values)
    {
        return (Visibility)new AnyTrueToVisibilityConverter().Convert(
            values, typeof(Visibility), null!, CultureInfo.InvariantCulture);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Convert_WhenAnyFlagIsTrue_ShouldShowBanner(int trueIndex)
    {
        // Given banner flags where exactly one is true
        var values = new object?[] { false, false, false };
        values[trueIndex] = true;

        // When the banner visibility is evaluated
        // Then it is visible
        Assert.Equal(Visibility.Visible, Convert(values));
    }

    [Fact]
    public void Convert_WhenAllFlagsAreTrue_ShouldShowBanner()
    {
        // Given every banner flag set (never happens, but the converter must not care)
        // When the banner visibility is evaluated
        // Then it is visible
        Assert.Equal(Visibility.Visible, Convert(true, true, true));
    }

    [Fact]
    public void Convert_WhenNoFlagIsTrue_ShouldCollapseBanner()
    {
        // Given the banner dismissed (every state flag false)
        // When the banner visibility is evaluated
        // Then it takes no layout space
        Assert.Equal(Visibility.Collapsed, Convert(false, false, false));
    }

    [Fact]
    public void Convert_WhenInputsAreMalformed_ShouldCollapseBanner()
    {
        // Given missing or mistyped values (e.g. no data context yet)
        // When the banner visibility is evaluated
        // Then it never lingers visible on bad data
        Assert.Equal(Visibility.Collapsed, Convert());
        Assert.Equal(Visibility.Collapsed, Convert(null, null, null));
        Assert.Equal(Visibility.Collapsed, Convert("true", "false", 1));
    }

    [Fact]
    public void ConvertBack_ShouldThrowBecauseTheBindingIsOneWay()
    {
        // Given the one-way Visibility binding
        // When something tries to convert back
        // Then it fails loudly instead of corrupting state
        Assert.Throws<NotSupportedException>(() =>
            new AnyTrueToVisibilityConverter().ConvertBack(
                Visibility.Visible, new[] { typeof(bool), typeof(bool), typeof(bool) }, null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Converter_WhenInstantiatedByXaml_ShouldHaveParameterlessCtor()
    {
        // Given XAML instantiates converters through a parameterless ctor
        // When the type is created via Activator
        // Then it succeeds (BAML MissingMethodException regression pin)
        Assert.NotNull(Activator.CreateInstance(typeof(AnyTrueToVisibilityConverter)));
    }
}
