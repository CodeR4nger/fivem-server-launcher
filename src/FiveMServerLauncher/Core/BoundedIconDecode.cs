using System.Buffers.Binary;

namespace FiveMServerLauncher.Core;

public readonly record struct DecodeBounds(int? Width, int? Height)
{
    public static readonly DecodeBounds Natural = new(null, null);
}

public static class BoundedIconDecode
{
    // Icons render at 22x22; the cap keeps DPI headroom while bounding decode memory.
    public const int MaxPixelSize = 96;

    public static DecodeBounds GetDecodeBounds(byte[] bytes)
    {
        // Unparseable input still decodes bounded on both axes.
        if (bytes.Length < 24 || !HasPngSignature(bytes))
        {
            return new DecodeBounds(MaxPixelSize, MaxPixelSize);
        }

        // PNG IHDR: big-endian width at offset 16, height at offset 20.
        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20));

        if (width == 0 || height == 0)
        {
            return new DecodeBounds(MaxPixelSize, MaxPixelSize);
        }

        var largest = (long)Math.Max(width, height);

        if (largest <= MaxPixelSize)
        {
            return DecodeBounds.Natural;
        }

        // Bound BOTH axes (WPF scales by the smaller ratio): bounding only the
        // width would let an extreme-aspect bomb decode a skyscraper — a 1x(2^31-1)
        // PNG clamped to DecodePixelWidth=1 still decodes ~2^31 pixels tall.
        return new DecodeBounds(
            Math.Max(1, (int)((long)width * MaxPixelSize / largest)),
            Math.Max(1, (int)((long)height * MaxPixelSize / largest)));
    }

    private static bool HasPngSignature(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return bytes.AsSpan(0, 8).SequenceEqual(signature);
    }
}
