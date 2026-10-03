using System.Buffers.Binary;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Tests.Core;

public class BoundedIconDecodeTests
{
    [Fact]
    public void GetDecodeBounds_WhenPngWithinCap_ShouldDecodeNaturally()
    {
        // Given — small icons keep their natural pixels (no upscale, no re-decode cost).
        // When / Then
        var natural = BoundedIconDecode.GetDecodeBounds(PngHeader(1, 1));
        Assert.Null(natural.Width);
        Assert.Null(natural.Height);
        Assert.True(BoundedIconDecode.GetDecodeBounds(PngHeader(96, 96)).Width is null);
    }

    [Fact]
    public void GetDecodeBounds_WhenSquarePngExceedsCap_ShouldBoundBothAxesAtCap()
    {
        // Given — a decompression-bomb-sized square PNG must decode bounded.
        // When / Then
        var bounds = BoundedIconDecode.GetDecodeBounds(PngHeader(512, 512));
        Assert.Equal(96, bounds.Width);
        Assert.Equal(96, bounds.Height);
    }

    [Fact]
    public void GetDecodeBounds_WhenRectangularPngExceedsCap_ShouldScaleByLargestDimension()
    {
        // Given — the largest dimension is bounded to the cap, the other proportionally.
        // When / Then
        var wide = BoundedIconDecode.GetDecodeBounds(PngHeader(512, 256));
        Assert.Equal(96, wide.Width);
        Assert.Equal(48, wide.Height);

        var tall = BoundedIconDecode.GetDecodeBounds(PngHeader(256, 512));
        Assert.Equal(48, tall.Width);
        Assert.Equal(96, tall.Height);
    }

    [Fact]
    public void GetDecodeBounds_WhenExtremeAspectBomb_ShouldClampShortAxisToMinimum()
    {
        // Given — extreme aspect ratios must stay bounded on BOTH axes: the short
        // axis clamps to at least one pixel while the long axis is bounded by the cap.
        // When / Then
        var tall = BoundedIconDecode.GetDecodeBounds(PngHeader(8, 20000));
        Assert.Equal(1, tall.Width);
        Assert.Equal(96, tall.Height);

        var wide = BoundedIconDecode.GetDecodeBounds(PngHeader(20000, 8));
        Assert.Equal(96, wide.Width);
        Assert.Equal(1, wide.Height);
    }

    [Fact]
    public void GetDecodeBounds_WhenMaxUintBombDimension_ShouldStillBoundBothAxes()
    {
        // Given — the escape regime that motivated the two-axis bound: a 1 x 2^31-1
        // PNG clamping only DecodePixelWidth would decode ~2^31 pixels tall.
        // When / Then
        var tall = BoundedIconDecode.GetDecodeBounds(PngHeader(1, 2147483647));
        Assert.Equal(1, tall.Width);
        Assert.Equal(96, tall.Height);

        var wide = BoundedIconDecode.GetDecodeBounds(PngHeader(2147483647, 1));
        Assert.Equal(96, wide.Width);
        Assert.Equal(1, wide.Height);
    }

    [Fact]
    public void GetDecodeBounds_WhenNotPngBytes_ShouldBoundBothAxesAtCap()
    {
        // Given — unknown formats still decode bounded on both axes.
        var bytes = new byte[100];

        // When / Then
        var bounds = BoundedIconDecode.GetDecodeBounds(bytes);
        Assert.Equal(96, bounds.Width);
        Assert.Equal(96, bounds.Height);
    }

    [Fact]
    public void GetDecodeBounds_WhenTooShortForHeader_ShouldBoundBothAxesAtCap()
    {
        // Given / When / Then
        var bounds = BoundedIconDecode.GetDecodeBounds(new byte[10]);
        Assert.Equal(96, bounds.Width);
        Assert.Equal(96, bounds.Height);
    }

    [Fact]
    public void GetDecodeBounds_WhenHeaderClaimsZeroDimensions_ShouldBoundBothAxesAtCap()
    {
        // Given — an invalid header falls back to a bounded decode.
        // When / Then
        var bounds = BoundedIconDecode.GetDecodeBounds(PngHeader(0, 0));
        Assert.Equal(96, bounds.Width);
        Assert.Equal(96, bounds.Height);
    }

    private static byte[] PngHeader(uint width, uint height)
    {
        var bytes = new byte[29];
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(bytes);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(8), 13);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16), width);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20), height);
        bytes[24] = 8; // bit depth
        bytes[25] = 6; // color type RGBA
        return bytes;
    }
}
