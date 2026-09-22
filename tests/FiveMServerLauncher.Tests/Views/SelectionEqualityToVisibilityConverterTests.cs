using System.Globalization;
using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class SelectionEqualityToVisibilityConverterTests
{
    [Fact]
    public void Convert_WhenValuesReferenceEqual_ShouldReturnVisible()
    {
        // Given
        var same = new object();
        var converter = new SelectionEqualityToVisibilityConverter();

        // When / Then
        Assert.Equal(Visibility.Visible, converter.Convert(
            new[] { same, same }, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_WhenValuesDiffer_ShouldReturnCollapsed()
    {
        // Given
        var converter = new SelectionEqualityToVisibilityConverter();

        // When / Then
        Assert.Equal(Visibility.Collapsed, converter.Convert(
            new[] { new object(), new object() }, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_WhenValuesEqualButNotReferenceEqual_ShouldReturnCollapsed()
    {
        // Given: two equal strings that are different object references.
        var first = new string(new[] { 'a' });
        var second = new string(new[] { 'a' });
        var converter = new SelectionEqualityToVisibilityConverter();

        // When / Then
        Assert.Equal(Visibility.Collapsed, converter.Convert(
            new[] { first, second }, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_WhenSameReferenceNull_ShouldReturnVisible()
    {
        // Given
        var converter = new SelectionEqualityToVisibilityConverter();

        // When / Then
        Assert.Equal(Visibility.Visible, converter.Convert(
            new object?[] { null, null }!, typeof(Visibility), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotSupported()
    {
        // Given
        var converter = new SelectionEqualityToVisibilityConverter();

        // When / Then
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(Visibility.Visible, new[] { typeof(object), typeof(object) }, null!, CultureInfo.InvariantCulture));
    }
}