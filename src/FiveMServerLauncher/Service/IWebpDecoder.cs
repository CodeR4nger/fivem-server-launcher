namespace FiveMServerLauncher.Service;

/// <summary>
/// The decoded first frame of a WebP image: tightly packed BGRA rows,
/// <see cref="Stride"/> = width * 4 (libwebp's simple decode API).
/// </summary>
public sealed record WebpFrame(int Width, int Height, byte[] Bgra)
{
    public int Stride => Width * 4;
}

/// <summary>
/// Seam over the native libwebp simple API. The real implementation is thin
/// P/Invoke glue (untested per the process-starter convention); the logic around
/// it lives in <see cref="IconImageNormalizer"/> and is tested against fakes,
/// with a real-fixture end-to-end test pinning the native library in-process.
/// </summary>
public interface IWebpDecoder
{
    /// <summary>
    /// Reads the canvas dimensions without decoding pixels (WebPGetInfo).
    /// Null when the bytes are not decodable WebP.
    /// </summary>
    (int Width, int Height)? GetDimensions(byte[] webp);

    /// <summary>
    /// Decodes the first frame to BGRA (WebPDecodeBGRA). Callers must have
    /// bounds-checked <see cref="GetDimensions"/> first — the raw buffer is
    /// width * height * 4 before any WPF bound applies.
    /// </summary>
    WebpFrame? Decode(byte[] webp);
}
