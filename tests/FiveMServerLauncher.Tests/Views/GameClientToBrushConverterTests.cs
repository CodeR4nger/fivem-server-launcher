using System.Globalization;
using System.Windows.Media;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class GameClientToBrushConverterTests
{
    private const string Orange = "#FFF5A623";
    private const string Blue = "#FF4DA3FF";
    private const string Red = "#FFE05252";
    private const string Grey = "#FF3A3A3A";

    private static string ArgbHex(SolidColorBrush brush) =>
        $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";

    private static SolidColorBrush Convert(object? value)
    {
        return (SolidColorBrush)new GameClientToBrushConverter().Convert(
            value!, typeof(Brush), null!, CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Convert_WhenFiveM_ShouldReturnOrange()
    {
        // Given / When / Then
        Assert.Equal(Orange, ArgbHex(Convert(GameClient.FiveM)));
    }

    [Fact]
    public void Convert_WhenFiveMEnhanced_ShouldReturnBlue()
    {
        // Given / When / Then
        Assert.Equal(Blue, ArgbHex(Convert(GameClient.FiveMEnhanced)));
    }

    [Fact]
    public void Convert_WhenRedM_ShouldReturnRed()
    {
        // Given / When / Then
        Assert.Equal(Red, ArgbHex(Convert(GameClient.RedM)));
    }

    [Fact]
    public void Convert_WhenUnmappedValue_ShouldReturnGreyFallback()
    {
        // Given
        var unmapped = (GameClient)999;

        // When / Then
        Assert.Equal(Grey, ArgbHex(Convert(unmapped)));
    }

    [Fact]
    public void Convert_WhenNullValue_ShouldReturnGreyFallback()
    {
        // When / Then
        Assert.Equal(Grey, ArgbHex(Convert(null)));
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotSupported()
    {
        // Given
        var converter = new GameClientToBrushConverter();

        // When / Then
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(Brushes.Orange, typeof(GameClient), null!, CultureInfo.InvariantCulture));
    }
}