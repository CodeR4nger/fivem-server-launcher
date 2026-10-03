using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Configuration;

namespace FiveMServerLauncher.Tests.Service;

public class ShortcutIconWriterTests
{
    private static readonly byte[] Png1x1 = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    private static byte[] GeneratePng(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        var source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    [Fact]
    public void TryWrite_WithValidPng_ShouldWriteIcoContainerWithEmbeddedPng()
    {
        // Given
        using var directory = new TempSettingsDirectory();
        var writer = new ShortcutIconWriter(() => directory.DirectoryPath);

        // When
        var path = writer.TryWrite(Png1x1);

        // Then — the container is a parseable single-image ICO wrapping the PNG.
        Assert.NotNull(path);
        Assert.True(File.Exists(path));
        Assert.EndsWith(".ico", path);

        var ico = File.ReadAllBytes(path);
        Assert.Equal(0, ico[0]);
        Assert.Equal(0, ico[1]);
        Assert.Equal(1, ico[2]);
        Assert.Equal(0, ico[3]);
        Assert.Equal(1, ico[4]);
        Assert.Equal(0, ico[5]);
        Assert.Equal(1, ico[6]);
        Assert.Equal(1, ico[7]);
        Assert.Equal(22, BitConverter.ToInt32(ico, 18));

        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        Assert.Equal(pngSignature, ico[22..30]);
        Assert.Equal(BitConverter.ToInt32(ico, 14), ico.Length - 22);
    }

    [Fact]
    public void TryWrite_WithSameBytesTwice_ShouldDedupeToOneHashFile()
    {
        // Given — identical artwork is written once, whatever server it came from.
        using var directory = new TempSettingsDirectory();
        var writer = new ShortcutIconWriter(() => directory.DirectoryPath);

        // When
        var first = writer.TryWrite(Png1x1);
        var second = writer.TryWrite(Png1x1);

        // Then
        Assert.Equal(first, second);
        var files = Directory.GetFiles(directory.DirectoryPath);
        Assert.Single(files);
    }

    [Fact]
    public void TryWrite_WithDifferentBytes_ShouldWriteDistinctHashFiles()
    {
        // Given
        using var directory = new TempSettingsDirectory();
        var writer = new ShortcutIconWriter(() => directory.DirectoryPath);
        var other = GeneratePng(width: 2, height: 1);

        // When
        var first = writer.TryWrite(Png1x1);
        var second = writer.TryWrite(other);

        // Then — distinct content hashes into distinct 64-hex-char names.
        Assert.NotEqual(first, second);
        Assert.Equal(2, Directory.GetFiles(directory.DirectoryPath).Length);
        Assert.Matches("^[0-9A-F]{64}\\.ico$", Path.GetFileName(first));
    }

    [Fact]
    public void TryWrite_WithOversizedPng_ShouldNormalizeThroughBoundedDecode()
    {
        // Given — artwork above the 96 px cap decodes bounded, both axes,
        // aspect preserved: 200x200 lands at 96x96 inside the ICO entry.
        using var directory = new TempSettingsDirectory();
        var writer = new ShortcutIconWriter(() => directory.DirectoryPath);

        // When
        var path = writer.TryWrite(GeneratePng(width: 200, height: 200));

        // Then
        Assert.NotNull(path);
        var ico = File.ReadAllBytes(path);
        Assert.Equal(96, ico[6]);
        Assert.Equal(96, ico[7]);
    }

    [Fact]
    public void TryWrite_WithGarbageBytes_ShouldReturnNullWithoutWriting()
    {
        // Given — undecodable bytes cannot become an icon: launcher-icon fallback.
        using var directory = new TempSettingsDirectory();
        var writer = new ShortcutIconWriter(() => directory.DirectoryPath);

        // When
        var path = writer.TryWrite(new byte[] { 1, 2, 3, 4, 5 });

        // Then
        Assert.Null(path);
        Assert.False(Directory.Exists(directory.DirectoryPath));
    }

    [Fact]
    public void TryWrite_WithUnwritableDirectory_ShouldReturnNull()
    {
        // Given — the icon directory path collides with an existing file.
        using var directory = new TempSettingsDirectory();
        var blocker = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        File.WriteAllText(blocker, "in the way");
        var writer = new ShortcutIconWriter(() => blocker);

        // When
        var path = writer.TryWrite(Png1x1);

        // Then — the shortcut can still be created with the launcher icon.
        Assert.Null(path);
        File.Delete(blocker);
    }

    [Theory]
    [InlineData(new byte[] { })]
    public void TryWrite_WithNullOrEmptyBytes_ShouldReturnNull(byte[] bytes)
    {
        // Given
        using var directory = new TempSettingsDirectory();
        var writer = new ShortcutIconWriter(() => directory.DirectoryPath);

        // When / Then
        Assert.Null(writer.TryWrite(bytes));
        Assert.Null(writer.TryWrite(null!));
    }
}
