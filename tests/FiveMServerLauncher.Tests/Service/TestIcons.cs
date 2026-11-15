using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FiveMServerLauncher.Tests.Service;

// Real image bytes for icon tests: the pipeline proves decodability at download,
// so a fake payload like { 1, 2, 3 } is (correctly) not an icon.
internal static class TestIcons
{
    public static readonly byte[] Html = Encoding.ASCII.GetBytes(
        "<!DOCTYPE html><html><head><title>404 Not Found</title></head><body>nope</body></html>");

    public static readonly byte[] Garbage = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];

    // A valid signature and IHDR with the pixel data cut off.
    public static readonly byte[] CorruptPng = Png(1).AsSpan(0, 40).ToArray();

    // A distinct, valid 1x1 PNG per shade.
    public static byte[] Png(byte shade) => Encode(new PngBitmapEncoder(), 1, 1, shade);

    // An uncompressed bitmap: a valid image of a predictable, large byte size.
    public static byte[] Bmp(int width, int height) => Encode(new BmpBitmapEncoder(), width, height, 0);

    private static byte[] Encode(BitmapEncoder encoder, int width, int height, byte shade)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];
        Array.Fill(pixels, shade);

        var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
