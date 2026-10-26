using System.Runtime.InteropServices;

namespace FiveMServerLauncher.Tests.Service;

/// <summary>
/// Test-only WebP fixture generator riding libwebp's own simple encoder
/// (WebPEncodeRGBA) through the same native library the product decodes with.
/// Mirrors the shape of the production LibWebpDecoder P/Invoke glue.
/// </summary>
internal static class TestWebpEncoder
{
    [DllImport("libwebp", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint WebPEncodeRGBA(byte[] rgba, int width, int height, int stride, float qualityFactor, out nint output);

    [DllImport("libwebp", CallingConvention = CallingConvention.Cdecl)]
    private static extern void WebPFree(nint pointer);

    public static byte[] EncodeRgba(int width, int height)
    {
        var rgba = new byte[width * height * 4];

        for (var i = 0; i < rgba.Length; i++)
        {
            rgba[i] = (byte)(i % 251);
        }

        var size = WebPEncodeRGBA(rgba, width, height, width * 4, 90f, out var output);

        if (size == 0)
        {
            throw new InvalidOperationException("libwebp encode failed");
        }

        try
        {
            var result = new byte[(int)size];
            Marshal.Copy(output, result, 0, (int)size);
            return result;
        }
        finally
        {
            WebPFree(output);
        }
    }
}
