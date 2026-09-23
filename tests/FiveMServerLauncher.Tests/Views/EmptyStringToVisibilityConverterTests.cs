using System.Globalization;
using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class EmptyStringToVisibilityConverterTests
{
    [Fact]
    public void Convert_WhenNull_ShouldReturnVisible()
    {
        // Given / When / Then
        Assert.Equal(Visibility.Visible, Convert(null));
    }

    [Fact]
    public void Convert_WhenEmpty_ShouldReturnVisible()
    {
        // Given / When / Then
        Assert.Equal(Visibility.Visible, Convert(string.Empty));
    }

    [Fact]
    public void Convert_WhenWhitespace_ShouldReturnCollapsed()
    {
        // Given / When / Then — only truly empty shows the placeholder
        Assert.Equal(Visibility.Collapsed, Convert(" "));
    }

    [Fact]
    public void Convert_WhenNonEmpty_ShouldReturnCollapsed()
    {
        // Given / When / Then
        Assert.Equal(Visibility.Collapsed, Convert("alpha"));
    }

    [Fact]
    public void Convert_WhenNonString_ShouldReturnCollapsed()
    {
        // Given / When / Then
        Assert.Equal(Visibility.Collapsed, Convert(42));
    }

    private static Visibility Convert(object? value)
    {
        return (Visibility)new EmptyStringToVisibilityConverter().Convert(
            value, typeof(Visibility), null!, CultureInfo.InvariantCulture);
    }
}
