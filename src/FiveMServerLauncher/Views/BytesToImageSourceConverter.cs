using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using FiveMServerLauncher.Core;
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
    // A failed decode is cached too (as null): the same undecodable byte[] is
    // never re-decoded per realization, and never throws out of a binding.
    private readonly LruCache<byte[], BitmapImage?> _decoded;
    private readonly Func<byte[], BitmapImage?> _decode;

    public BytesToImageSourceConverter()
        : this(DefaultMaxCachedDecodes)
    {
    }

    public BytesToImageSourceConverter(int maxCachedDecodes, Func<byte[], BitmapImage?>? decode = null)
    {
        _decoded = new LruCache<byte[], BitmapImage?>(Math.Max(1, maxCachedDecodes));
        _decode = decode ?? BoundedIconDecode.TryDecode;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
        {
            return null!;
        }

        return _decoded.GetOrAdd(bytes, _decode)!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
