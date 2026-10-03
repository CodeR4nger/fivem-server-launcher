using System.IO;
using System.Security.Cryptography;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

public sealed class ShortcutIconWriter(Func<string> iconDirectoryProvider)
{
    public string? TryWrite(byte[]? iconBytes)
    {
        if (iconBytes is null || iconBytes.Length == 0)
        {
            return null;
        }

        try
        {
            var directory = iconDirectoryProvider();

            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            var frame = DecodeBounded(iconBytes);

            Directory.CreateDirectory(directory);

            var hash = Convert.ToHexString(SHA256.HashData(iconBytes));
            var path = Path.Combine(directory, hash + ".ico");

            if (File.Exists(path))
            {
                return path;
            }

            var png = EncodePng(frame);
            var container = BuildIcoContainer(png, frame.PixelWidth, frame.PixelHeight);

            // Stage-then-move: a crash mid-write must not leave a corrupt hash
            // file behind that dedup would serve forever.
            var staged = Path.Combine(directory, hash + "." + Guid.NewGuid() + ".tmp");
            File.WriteAllBytes(staged, container);
            File.Move(staged, path);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapSource DecodeBounded(byte[] bytes)
    {
        var bounds = BoundedIconDecode.GetDecodeBounds(bytes);

        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(bytes);

        if (bounds.Width is { } width)
        {
            image.DecodePixelWidth = width;
        }

        if (bounds.Height is { } height)
        {
            image.DecodePixelHeight = height;
        }

        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static byte[] EncodePng(BitmapSource frame)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(frame));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static byte[] BuildIcoContainer(byte[] png, int width, int height)
    {
        var container = new byte[22 + png.Length];
        container[2] = 1;
        container[4] = 1;
        container[6] = (byte)(width is >= 1 and <= 255 ? width : 0);
        container[7] = (byte)(height is >= 1 and <= 255 ? height : 0);
        container[10] = 1;
        container[12] = 32;
        WriteUInt32LittleEndian(container, 14, png.Length);
        WriteUInt32LittleEndian(container, 18, 22);
        png.CopyTo(container, 22);
        return container;
    }

    private static void WriteUInt32LittleEndian(byte[] target, int offset, int value)
    {
        target[offset] = (byte)value;
        target[offset + 1] = (byte)(value >> 8);
        target[offset + 2] = (byte)(value >> 16);
        target[offset + 3] = (byte)(value >> 24);
    }
}
