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

    // A real 128x128 solid-color PNG (bigger than the decode cap).
    private static readonly byte[] LargePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAIAAAACACAYAAADDPmHLAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAFNSURBVHhe7dIhAQAwDMCw+fdxnRu/hQaEFHf2zdI1f6DFAHEGiDNAnAHiDBBngDgDxBkgzgBxBogzQJwB4gwQZ4A4A8QZIM4AcQaIM0CcAeIMEGeAOAPEGSDOAHEGiDNAnAHiDBBngDgDxBkgzgBxBogzQJwB4gwQZ4A4A8QZIM4AcQaIM0CcAeIMEGeAOAPEGSDOAHEGiDNAnAHiDBBngDgDxBkgzgBxBogzQJwB4gwQZ4A4A8QZIM4AcQaIM0CcAeIMEGeAOAPEGSDOAHEGiDNAnAHiDBBngDgDxBkgzgBxBogzQJwB4gwQZ4A4A8QZIM4AcQaIM0CcAeIMEGeAOAPEGSDOAHEGiDNAnAHiDBBngDgDxBkgzgBxBogzQJwB4gwQZ4A4A8QZIM4AcQaIM0CcAeIMEGeAOAPEGSDOAHEGiDNAnAHiDBBngLgDBObJ2bwxSEUAAAAASUVORK5CYII=");

    [Fact]
    public void Convert_WhenPngExceedsDecodeCap_ShouldDecodeBounded()
    {
        // Given — a server-published bomb-sized PNG decodes at a bounded pixel size.
        var converter = new BytesToImageSourceConverter();

        // When
        var result = converter.Convert(LargePng, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);

        // Then
        var image = Assert.IsType<BitmapImage>(result);
        Assert.True(image.IsFrozen);
        Assert.Equal(96, image.PixelWidth);
        Assert.Equal(96, image.PixelHeight);
    }

    // A real 4x512 tall PNG (long axis beyond the decode cap).
    private static readonly byte[] TallPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAQAAAIACAYAAACl/81BAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAA1SURBVGhD7cghDQAACAAw0hGMTPQDSwW2i5tHVs8VQgghhBBCCCGEEEIIIYQQQgghhBDiUyxyvtkes6Fk9QAAAABJRU5ErkJggg==");

    [Fact]
    public void Convert_WhenSameBytesRebound_ShouldReuseDecodedImage()
    {
        // Given — scrolling re-realizes rows and re-evaluates the binding with the
        // same byte[] instance; the decode happens once, not per realization.
        var converter = new BytesToImageSourceConverter();

        // When
        var first = converter.Convert(TinyPng, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);
        var second = converter.Convert(TinyPng, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);

        // Then
        Assert.Same(first, second);
    }

    [Fact]
    public void Convert_WhenCacheExceedsCapacity_ShouldEvictLeastRecentlyUsedDecode()
    {
        // Given — the decoded-image cache is session-bounded, not app-lifetime growth:
        // beyond capacity the least recently used decode is dropped and re-decoded.
        var first = new BytesToImageSourceConverter(maxCachedDecodes: 2);
        var alpha = Convert.FromBase64String(Convert.ToBase64String(TinyPng));
        var beta = Convert.FromBase64String(Convert.ToBase64String(TinyPng));
        var gamma = Convert.FromBase64String(Convert.ToBase64String(TinyPng));

        // When
        var a1 = first.Convert(alpha, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);
        _ = first.Convert(beta, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);
        _ = first.Convert(gamma, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);
        var a2 = first.Convert(alpha, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);

        // Then — alpha was evicted by the capacity overflow and decoded anew.
        Assert.NotSame(a1, a2);
    }

    [Fact]
    public void Convert_WhenTallPngExceedsDecodeCap_ShouldBoundLongAxis()
    {
        // Given — a tall bomb-shaped PNG: bounding only the width would decode a
        // skyscraper; the converter must bound the long (height) axis.
        var converter = new BytesToImageSourceConverter();

        // When
        var result = converter.Convert(TallPng, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);

        // Then
        var image = Assert.IsType<BitmapImage>(result);
        Assert.True(image.IsFrozen);
        Assert.True(image.PixelHeight <= 96, $"decoded height {image.PixelHeight} exceeded the cap");
        Assert.True(image.PixelWidth <= 96, $"decoded width {image.PixelWidth} exceeded the cap");
    }

    [Fact]
    public void Convert_WhenDifferentBytes_ShouldDecodeSeparateImages()
    {
        // Given / When
        var converter = new BytesToImageSourceConverter();
        var first = converter.Convert(TinyPng, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);
        var second = converter.Convert(LargePng, typeof(BitmapImage), null!, CultureInfo.InvariantCulture);

        // Then
        Assert.NotSame(first, second);
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

    [Fact]
    public void Converter_WhenConstructedViaDefaultCtorReflection_ShouldBeConstructible()
    {
        // Given — XAML (BAML) instantiates StaticResource converters through the
        // reflection default-ctor lookup; an all-optional-parameter ctor passes C#
        // but throws MissingMethodException at runtime on window load.
        // When / Then
        Assert.NotNull(Activator.CreateInstance(typeof(BytesToImageSourceConverter)));
    }
}