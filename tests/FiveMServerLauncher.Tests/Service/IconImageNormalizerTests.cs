using System.IO;
using System.Windows.Media.Imaging;
using FiveMServerLauncher.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.Service;

public class IconImageNormalizerTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static byte[] RealCfxIcon() =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Service", "Fixtures", "cfx-server-icon.webp"));

    private static (int Width, int Height) PngDimensions(byte[] png)
    {
        var frame = BitmapFrame.Create(new MemoryStream(png), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        return (frame.PixelWidth, frame.PixelHeight);
    }

    private static void AssertIsPng(byte[]? bytes)
    {
        Assert.NotNull(bytes);
        Assert.True(bytes!.Length > PngSignature.Length);
        Assert.True(bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature), "expected a PNG signature");
    }

    [Fact]
    public void Normalize_WithRealCfxWebpIcon_ShouldDecodeToBoundedPng()
    {
        // Given — a real production icon captured from cfx-server-metadata (VP8X
        // 96x96, 2.8 KB). This pins that the native libwebp shipped through the
        // runtime package actually decodes inside our process, and that the output
        // is a PNG WPF can render — the exact gap that broke every icon.
        var normalizer = new IconImageNormalizer(new LibWebpDecoder());

        // When
        var png = normalizer.Normalize(RealCfxIcon());

        // Then
        AssertIsPng(png);
        var (width, height) = PngDimensions(png!);
        Assert.Equal(96, width);
        Assert.Equal(96, height);
    }

    [Fact]
    public void Normalize_WhenRealWebpExceedsBombCap_ShouldReturnNull()
    {
        // Given — a real WebP whose canvas (2000x2000, generated at test time via
        // libwebp's own encoder) exceeds the gate: the header must be read and
        // rejected through the REAL decoder, never decoding the raw buffer.
        var webp = TestWebpEncoder.EncodeRgba(2000, 2000);
        var normalizer = new IconImageNormalizer(new LibWebpDecoder());

        // When
        var png = normalizer.Normalize(webp);

        // Then
        Assert.Null(png);
    }

    [Fact]
    public void Normalize_WhenBytesAreDecodableNonWebp_ShouldPassThroughSameInstanceWithoutWebpDecoding()
    {
        // Given — WPF-native formats that really decode keep the existing render
        // path untouched and never reach the WebP decoder.
        var normalizer = new IconImageNormalizer(new ThrowingWebpDecoder());
        var png = TestIcons.Png(7);

        // When
        var result = normalizer.Normalize(png);

        // Then — the same instance.
        Assert.Same(png, result);
    }

    [Fact]
    public void Normalize_WhenNonWebpBytesAreNotAnImage_ShouldReturnNull()
    {
        // Given — a server can publish an icon_url that answers with anything: an
        // HTML error page, random bytes, a truncated PNG. Handing those to WPF
        // throws out of the Image.Source binding, so they must die here, at the
        // download boundary, as "no icon".
        var normalizer = new IconImageNormalizer(new ThrowingWebpDecoder());

        // When / Then
        Assert.Null(normalizer.Normalize(TestIcons.Html));
        Assert.Null(normalizer.Normalize(TestIcons.Garbage));
        Assert.Null(normalizer.Normalize(TestIcons.CorruptPng));
    }

    [Fact]
    public void Normalize_WhenCanvasExceedsBombCap_ShouldReturnNullWithoutDecoding()
    {
        // Given — the raw BGRA buffer is allocated before any WPF bound applies,
        // so a hostile canvas must be rejected at the header, never decoded.
        var decoder = new FakeWebpDecoder { Dimensions = (2048, 64) };
        var normalizer = new IconImageNormalizer(decoder);

        // When
        var wideBomb = normalizer.Normalize(WebpWithMagic);

        // Then
        Assert.Null(wideBomb);
        Assert.Equal(0, decoder.DecodeCalls);

        // Given — the tall-axis bomb too.
        decoder.Dimensions = (64, 2048);

        // When
        var tallBomb = normalizer.Normalize(WebpWithMagic);

        // Then
        Assert.Null(tallBomb);
        Assert.Equal(0, decoder.DecodeCalls);
    }

    [Fact]
    public void Normalize_WhenDimensionsUnreadable_ShouldReturnNull()
    {
        // Given — corrupt or truncated WebP: no canvas dimensions, no icon.
        var decoder = new FakeWebpDecoder { Dimensions = null };
        var normalizer = new IconImageNormalizer(decoder);

        // When
        var result = normalizer.Normalize(WebpWithMagic);

        // Then
        Assert.Null(result);
        Assert.Equal(0, decoder.DecodeCalls);
    }

    [Fact]
    public void Normalize_WhenDecodeFails_ShouldReturnNull()
    {
        // Given — dimensions read fine but the pixel decode fails.
        var decoder = new FakeWebpDecoder { Dimensions = (64, 64), Frame = null };
        var normalizer = new IconImageNormalizer(decoder);

        // When
        var result = normalizer.Normalize(WebpWithMagic);

        // Then
        Assert.Null(result);
        Assert.Equal(1, decoder.DecodeCalls);
    }

    [Fact]
    public void Normalize_WhenWebpExceedsRenderHeadroom_ShouldScaleToMaxAxis96()
    {
        // Given — icons render at 22x22; anything over the 96px headroom is
        // scaled down before re-encoding.
        var decoder = new FakeWebpDecoder
        {
            Dimensions = (400, 400),
            Frame = new WebpFrame(400, 400, new byte[400 * 400 * 4])
        };
        var normalizer = new IconImageNormalizer(decoder);

        // When
        var png = normalizer.Normalize(WebpWithMagic);

        // Then
        AssertIsPng(png);
        Assert.Equal((96, 96), PngDimensions(png!));
    }

    [Fact]
    public void Normalize_WhenWebpFitsUnderHeadroom_ShouldKeepNaturalDimensions()
    {
        // Given — a small icon decodes at its natural size; no upscale, no waste.
        var decoder = new FakeWebpDecoder
        {
            Dimensions = (64, 64),
            Frame = new WebpFrame(64, 64, new byte[64 * 64 * 4])
        };
        var normalizer = new IconImageNormalizer(decoder);

        // When
        var png = normalizer.Normalize(WebpWithMagic);

        // Then
        AssertIsPng(png);
        Assert.Equal((64, 64), PngDimensions(png!));
    }

    [Fact]
    public void Normalize_WhenWebpIsRectangle_ShouldPreserveAspectWithinMaxAxis()
    {
        // Given — the 96px cap applies to the larger axis; the smaller follows the
        // aspect ratio (mirrors BoundedIconDecode's both-axes bound).
        var decoder = new FakeWebpDecoder
        {
            Dimensions = (400, 100),
            Frame = new WebpFrame(400, 100, new byte[400 * 100 * 4])
        };
        var normalizer = new IconImageNormalizer(decoder);

        // When — wide
        var wide = normalizer.Normalize(WebpWithMagic);

        // Then
        AssertIsPng(wide);
        Assert.Equal((96, 24), PngDimensions(wide!));

        // Given — tall
        decoder.Dimensions = (100, 400);
        decoder.Frame = new WebpFrame(100, 400, new byte[100 * 400 * 4]);

        // When
        var tall = normalizer.Normalize(WebpWithMagic);

        // Then
        AssertIsPng(tall);
        Assert.Equal((24, 96), PngDimensions(tall!));
    }

    private static readonly byte[] WebpWithMagic =
    [
        (byte)'R', (byte)'I', (byte)'F', (byte)'F', 0x00, 0x00, 0x00, 0x00,
        (byte)'W', (byte)'E', (byte)'B', (byte)'P'
    ];

    private sealed class FakeWebpDecoder : IWebpDecoder
    {
        public (int Width, int Height)? Dimensions { get; set; }

        public WebpFrame? Frame { get; set; }

        public int DecodeCalls { get; private set; }

        public (int Width, int Height)? GetDimensions(byte[] webp) => Dimensions;

        public WebpFrame? Decode(byte[] webp)
        {
            DecodeCalls++;
            return Frame;
        }
    }

    private sealed class ThrowingWebpDecoder : IWebpDecoder
    {
        public (int Width, int Height)? GetDimensions(byte[] webp) =>
            throw new InvalidOperationException("non-WebP bytes must never reach the decoder");

        public WebpFrame? Decode(byte[] webp) =>
            throw new InvalidOperationException("non-WebP bytes must never reach the decoder");
    }
}
