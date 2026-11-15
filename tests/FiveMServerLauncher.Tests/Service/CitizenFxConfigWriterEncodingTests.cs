using System.Text;
using FiveMServerLauncher.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.Service;

// FiveM reads and writes CitizenFX.ini only through Get/WritePrivateProfileString, which
// use the system ANSI code page for a file without a UTF-16LE BOM (see
// .scratch/connect-correctness/spec.md section 5). Every line the writer does not change
// must survive byte-exact, whatever its encoding.
public class CitizenFxConfigWriterEncodingTests
{
    private static readonly Encoding Windows1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;

    private static readonly Dictionary<string, string> NewBuild = new() { ["DefaultBuild"] = "3788" };

    [Fact]
    public async Task Apply_WhenAnsiFileHasNonAsciiPath_ShouldPreserveItsBytes()
    {
        // Given — FiveM wrote a 1252 file: 'é' is the single byte 0xE9 (invalid UTF-8).
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        const string before = "[Game]\r\nIVPath=C:\\Juegos\\José\\GTA V\r\nDefaultBuild=3258\r\n\r\n[Addons]\r\nNote=Ñandú";
        File.WriteAllBytes(temp.IniPath, Windows1252.GetBytes(before));

        // When
        await new CitizenFxConfigWriter(Windows1252).ApplyAsync(temp.IniPath, NewBuild);

        // Then
        Assert.Equal(
            Windows1252.GetBytes(before.Replace("DefaultBuild=3258", "DefaultBuild=3788")),
            File.ReadAllBytes(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenFileHasBytesOfAnotherEncoding_ShouldPreserveThemVerbatim()
    {
        // Given — bytes that are undefined in 1252 (0x81, 0x8D) and a UTF-8 sequence.
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        var odd = new byte[] { 0x81, 0x8D, 0xC3, 0xA9, 0xFF };
        var original = Concat(Ascii("[Game]\r\nOther="), odd, Ascii("\r\nDefaultBuild=3258"));
        File.WriteAllBytes(temp.IniPath, original);

        // When
        await new CitizenFxConfigWriter(Windows1252).ApplyAsync(temp.IniPath, NewBuild);

        // Then
        Assert.Equal(
            Concat(Ascii("[Game]\r\nOther="), odd, Ascii("\r\nDefaultBuild=3788")),
            File.ReadAllBytes(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenWritingNonAsciiValueToAnsiFile_ShouldEncodeItInTheAnsiCodePage()
    {
        // Given
        using var temp = new TempCitizenFxIni();

        // When — a new file is created ANSI, as FiveM would create it.
        await new CitizenFxConfigWriter(Windows1252).ApplyAsync(
            temp.IniPath, new Dictionary<string, string> { ["PoolSizesIncrease"] = "{\"Señal\":1}" });

        // Then
        Assert.Equal(
            Windows1252.GetBytes("[Game]\r\nPoolSizesIncrease={\"Señal\":1}"),
            File.ReadAllBytes(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenUtf8BomFile_ShouldKeepBomAndFindGameSectionOnFirstLine()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        File.WriteAllBytes(temp.IniPath, Concat(bom, Encoding.UTF8.GetBytes("[Game]\r\nIVPath=C:\\José\r\nDefaultBuild=3258")));

        // When
        await new CitizenFxConfigWriter(Windows1252).ApplyAsync(temp.IniPath, NewBuild);

        // Then
        Assert.Equal(
            Concat(bom, Encoding.UTF8.GetBytes("[Game]\r\nIVPath=C:\\José\r\nDefaultBuild=3788")),
            File.ReadAllBytes(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenUtf16LeBomFile_ShouldStayUtf16WithBom()
    {
        // Given — the profile API's Unicode mode.
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        var bom = new byte[] { 0xFF, 0xFE };
        File.WriteAllBytes(temp.IniPath, Concat(bom, Encoding.Unicode.GetBytes("[Game]\r\nIVPath=C:\\José\r\nDefaultBuild=3258")));

        // When
        await new CitizenFxConfigWriter(Windows1252).ApplyAsync(
            temp.IniPath, new Dictionary<string, string> { ["DefaultBuild"] = "3788", ["Note"] = "日本" });

        // Then
        Assert.Equal(
            Concat(bom, Encoding.Unicode.GetBytes("[Game]\r\nIVPath=C:\\José\r\nDefaultBuild=3788\r\nNote=日本")),
            File.ReadAllBytes(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WithDefaultConstructor_ShouldEncodeValuesInTheSystemAnsiCodePage()
    {
        // Given — the code page FiveM's profile API uses on this machine.
        using var temp = new TempCitizenFxIni();
        var systemAnsi = CodePagesEncodingProvider.Instance.GetEncoding(GetACP())!;

        // When
        await new CitizenFxConfigWriter().ApplyAsync(
            temp.IniPath, new Dictionary<string, string> { ["Note"] = "José" });

        // Then
        Assert.Equal(systemAnsi.GetBytes("[Game]\r\nNote=José"), File.ReadAllBytes(temp.IniPath));
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern int GetACP();

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static byte[] Concat(params byte[][] parts) => parts.SelectMany(part => part).ToArray();
}
