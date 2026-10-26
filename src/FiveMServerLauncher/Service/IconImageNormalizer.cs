using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

/// <summary>
/// Normalizes server-published icon bytes to PNG at the download boundary. CFX's
/// icon host (cfx-server-metadata) serves WebP — a format WPF/WIC cannot decode —
/// so WebP is decoded through <see cref="IWebpDecoder"/> after a canvas-dimension
/// bomb gate, scaled to the render headroom, and re-encoded as PNG; every
/// downstream consumer (row converter, <see cref="BoundedIconDecode"/>, shortcut
/// ICO writer) stays byte-format-agnostic. WPF-native formats pass through
/// untouched and keep their existing render path.
/// </summary>
public sealed class IconImageNormalizer(IWebpDecoder decoder)
{
    // Icons render at 22x22; the cap keeps DPI headroom while bounding decode
    // memory. Shared with the PNG-side bound so both formats agree.
    public const int MaxPixelSize = BoundedIconDecode.MaxPixelSize;

    // The raw BGRA decode buffer is width * height * 4 before any WPF bound
    // applies, so a hostile canvas must be rejected at the header — generous for
    // an asset rendered at 22 px (real CFX icons are 96x96 to 400x400).
    public const int MaxWebpAxis = 1024;

    private readonly IWebpDecoder _decoder = decoder;

    public static bool IsWebp(byte[] bytes)
    {
        if (bytes.Length < 12)
        {
            return false;
        }

        // "RIFF" magic at 0, "WEBP" format tag at 8.
        ReadOnlySpan<byte> riff = [(byte)'R', (byte)'I', (byte)'F', (byte)'F'];
        ReadOnlySpan<byte> webp = [(byte)'W', (byte)'E', (byte)'B', (byte)'P'];

        return bytes.AsSpan(0, 4).SequenceEqual(riff)
            && bytes.AsSpan(8, 4).SequenceEqual(webp);
    }

    /// <summary>
    /// Returns WPF-renderable PNG bytes, the input untouched when it is not WebP,
    /// or null when the bytes are WebP but undecodable, oversized, or hostile.
    /// Never throws.
    /// </summary>
    public byte[]? Normalize(byte[]? bytes)
    {
        try
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            return IsWebp(bytes) ? NormalizeWebp(bytes) : bytes;
        }
        catch
        {
            // The ShortcutIconWriter precedent: icon materialization must never
            // throw to its caller — a hostile, truncated or oversize input, or a
            // native-library fault, all degrade to "no icon".
            return null;
        }
    }

    private byte[]? NormalizeWebp(byte[] webp)
    {
        var dimensions = _decoder.GetDimensions(webp);

        if (dimensions is not { } canvas)
        {
            return null;
        }

        if (canvas.Width <= 0 || canvas.Height <= 0
            || canvas.Width > MaxWebpAxis || canvas.Height > MaxWebpAxis)
        {
            return null;
        }

        if (_decoder.Decode(webp) is not { } frame)
        {
            return null;
        }

        // Freezables created and frozen on this (download) thread are safe on
        // every thread afterwards — the same dance ShortcutIconWriter does.
        var source = BitmapSource.Create(
            frame.Width, frame.Height,
            96, 96,
            PixelFormats.Bgra32,
            null,
            frame.Bgra,
            frame.Stride);
        source.Freeze();

        var bounded = ScaleDown(source);

        return EncodePng(bounded);
    }

    private static BitmapSource ScaleDown(BitmapSource source)
    {
        var largest = Math.Max(source.PixelWidth, source.PixelHeight);

        if (largest <= MaxPixelSize)
        {
            return source;
        }

        var scale = (double)MaxPixelSize / largest;
        var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        transformed.Freeze();
        return transformed;
    }

    private static byte[] EncodePng(BitmapSource frame)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(frame));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
