using System.Globalization;
using System.Windows.Media.Imaging;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class BytesToImageSourceConverterTests
{
    // A tiny valid 1x1 transparent PNG.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    [Fact]
    public void Convert_WhenNull_ShouldReturnNull()
    {
        // When / Then
        Assert.Null(new BytesToImageSourceConverter().Convert(
            value: null!, typeof(BitmapImage), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_WhenEmptyBytes_ShouldReturnNull()
    {
        // Given / When / Then
        Assert.Null(new BytesToImageSourceConverter().Convert(
            Array.Empty<byte>(), typeof(BitmapImage), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_WhenNonByteArray_ShouldReturnNull()
    {
        // Given / When / Then
        Assert.Null(new BytesToImageSourceConverter().Convert(
            "not bytes", typeof(BitmapImage), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_WhenValidPngBytes_ShouldReturnFrozenBitmapImage()
    {
        // Given
        var converter = new BytesToImageSourceConverter();

        // When
        var result = converter.Convert(TinyPng, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);

        // Then
        var image = Assert.IsType<BitmapImage>(result);
        Assert.True(image.IsFrozen);
        Assert.Equal(1, image.PixelWidth);
        Assert.Equal(1, image.PixelHeight);
    }

    [Fact]
    public void ConvertBack_ShouldThrowNotSupported()
    {
        // Given
        var converter = new BytesToImageSourceConverter();

        // When / Then
        Assert.Throws<NotSupportedException>(() =>
            converter.ConvertBack(new BitmapImage(), typeof(byte[]), null!, CultureInfo.InvariantCulture));
    }
}