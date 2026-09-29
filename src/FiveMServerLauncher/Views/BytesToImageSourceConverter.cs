using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Views;

public sealed class BytesToImageSourceConverter : IValueConverter
{
    // Icons render at 22x22; the cap keeps DPI headroom over the decoded-image cache.
    private const int DefaultMaxCachedDecodes = 512;

    // Re-realized rows (browser scrolling, binding re-evaluation) pass the same byte[]
    // instance; the decoded image is reused instead of re-decoded per realization.
    // The cache is bounded (LRU) — scrolled-away decodes are evicted, so a long
    // browsing session never accumulates unbounded image memory. byte[] keys compare
    // by reference, matching the enrichment service's per-icon byte instances.
    private readonly LruCache<byte[], BitmapImage> _decoded;

    public BytesToImageSourceConverter()
        : this(DefaultMaxCachedDecodes)
    {
    }

    public BytesToImageSourceConverter(int maxCachedDecodes)
    {
        _decoded = new LruCache<byte[], BitmapImage>(Math.Max(1, maxCachedDecodes));
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
        {
            return null!;
        }

        return _decoded.GetOrAdd(bytes, Decode);
    }

    private static BitmapImage Decode(byte[] bytes)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;

        // Server-published icons decode at a bounded pixel size (decompression-bomb
        // protection); small PNGs keep their natural pixels.
        var bounds = BoundedIconDecode.GetDecodeBounds(bytes);

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

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
