using System.Buffers.Binary;
using System.IO;
using System.Windows.Media.Imaging;

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

    /// <summary>
    /// Decodes untrusted icon bytes into a frozen image at the bounded pixel size,
    /// or null when WPF cannot decode them (an HTML error page, garbage, a
    /// truncated file). Never throws: this is both the download-time decodability
    /// proof and the row converter's decode, and neither may fault on hostile
    /// input. Safe on any thread — the image is frozen before it is returned.
    /// </summary>
    public static BitmapImage? TryDecode(byte[] bytes)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;

            // Small PNGs keep their natural pixels; everything else is bounded.
            var bounds = GetDecodeBounds(bytes);

            if (bounds.Width is int width)
            {
                image.DecodePixelWidth = width;
            }

            if (bounds.Height is int height)
            {
                image.DecodePixelHeight = height;
            }

            image.StreamSource = new MemoryStream(bytes);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool HasPngSignature(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        return bytes.AsSpan(0, 8).SequenceEqual(signature);
    }
}
