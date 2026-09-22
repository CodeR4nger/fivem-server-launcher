using System.Globalization;
using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class InverseBoolToVisibilityConverterTests
{
    [Fact]
    public void Convert_WhenTrue_ShouldReturnCollapsed()
    {
        // Given / When / Then
        Assert.Equal(Visibility.Collapsed, Convert(true));
    }

    [Fact]
    public void Convert_WhenFalse_ShouldReturnVisible()
    {
        // Given / When / Then
        Assert.Equal(Visibility.Visible, Convert(false));
    }

    [Fact]
    public void Convert_WhenNonBoolean_ShouldReturnVisible()
    {
        // Given / When / Then
        Assert.Equal(Visibility.Visible, Convert("not-a-bool"));
    }

    [Theory]
    [InlineData(Visibility.Visible, false)]
    [InlineData(Visibility.Collapsed, true)]
    [InlineData(Visibility.Hidden, true)]
    public void ConvertBack_ShouldReturnBoolFromVisibility(Visibility visibility, bool expected)
    {
        // Given
        var converter = new InverseBoolToVisibilityConverter();

        // When / Then
        Assert.Equal(expected, converter.ConvertBack(
            visibility, typeof(bool), null!, CultureInfo.InvariantCulture));
    }

    private static Visibility Convert(object value)
    {
        return (Visibility)new InverseBoolToVisibilityConverter().Convert(
            value, typeof(Visibility), null!, CultureInfo.InvariantCulture);
    }
}