using System.Runtime.InteropServices;

namespace FiveMServerLauncher.Service;

/// <summary>
/// Real <see cref="IWebpDecoder"/> over the native libwebp shipped by the
/// Imazen.WebP.NativeRuntime.win-x64 package (official libwebp 1.6.1). Thin glue:
/// signatures mirror libwebp's simple API; on win-x64 there is a single calling
/// convention, so Cdecl/StdCall is a non-issue.
/// </summary>
public sealed class LibWebpDecoder : IWebpDecoder
{
    private static class Native
    {
        private const string Library = "libwebp";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WebPGetInfo(byte[] data, int dataSize, out int width, out int height);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern nint WebPDecodeBGRA(byte[] data, int dataSize, out int width, out int height);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void WebPFree(nint pointer);
    }

    public (int Width, int Height)? GetDimensions(byte[] webp)
    {
        if (webp.Length == 0 || Native.WebPGetInfo(webp, webp.Length, out var width, out var height) == 0)
        {
            return null;
        }

        return (width, height);
    }

    public WebpFrame? Decode(byte[] webp)
    {
        if (webp.Length == 0)
        {
            return null;
        }

        var pixels = Native.WebPDecodeBGRA(webp, webp.Length, out var width, out var height);

        if (pixels == nint.Zero || width <= 0 || height <= 0)
        {
            return null;
        }

        try
        {
            var bgra = new byte[width * height * 4];
            Marshal.Copy(pixels, bgra, 0, bgra.Length);
            return new WebpFrame(width, height, bgra);
        }
        finally
        {
            Native.WebPFree(pixels);
        }
    }
}
