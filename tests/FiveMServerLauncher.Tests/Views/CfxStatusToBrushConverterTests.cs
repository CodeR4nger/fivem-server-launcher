using System.Globalization;
using System.Windows.Media;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Views;
using Xunit;

namespace FiveMServerLauncher.Tests.Views;

public class CfxStatusToBrushConverterTests
{
    private const string Green = "#FF55C271";
    private const string Amber = "#FFF5A623";
    private const string Red = "#FFE05252";
    private const string Grey = "#FF3A3A3A";

    private static string ArgbHex(SolidColorBrush brush) =>
        $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";

    private static SolidColorBrush Convert(object? value)
    {
        return (SolidColorBrush)new CfxStatusToBrushConverter().Convert(
            value!, typeof(Brush), null!, CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Convert_WhenOperational_ShouldReturnGreen()
    {
        // Given / When / Then
        Assert.Equal(Green, ArgbHex(Convert(CfxStatus.Operational)));
    }

    [Fact]
    public void Convert_WhenDegraded_ShouldReturnAmber()
    {
        // Given / When / Then
        Assert.Equal(Amber, ArgbHex(Convert(CfxStatus.Degraded)));
    }

    [Fact]
    public void Convert_WhenPartialOutage_ShouldReturnRed()
    {
        // Given / When / Then
        Assert.Equal(Red, ArgbHex(Convert(CfxStatus.PartialOutage)));
    }

    [Fact]
    public void Convert_WhenMajorOutage_ShouldReturnRed()
    {
        // Given / When / Then
        Assert.Equal(Red, ArgbHex(Convert(CfxStatus.MajorOutage)));
    }

    [Fact]
    public void Convert_WhenMaintenance_ShouldReturnGrey()
    {
        // Given / When / Then
        Assert.Equal(Grey, ArgbHex(Convert(CfxStatus.Maintenance)));
    }

    [Fact]
    public void Convert_WhenUnknown_ShouldReturnGrey()
    {
        // Given / When / Then
        Assert.Equal(Grey, ArgbHex(Convert(CfxStatus.Unknown)));
    }

    [Fact]
    public void Convert_WhenUnmappedInt_ShouldReturnGreyFallback()
    {
        // Given
        var unmapped = (CfxStatus)999;

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
        var converter = new CfxStatusToBrushConverter();

        // When / Then
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(Brushes.Green, typeof(CfxStatus), null!, CultureInfo.InvariantCulture));
    }
}