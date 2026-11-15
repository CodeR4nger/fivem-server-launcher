using System.IO;
using System.Text;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

public interface ICitizenFxConfigWriter
{
    Task ApplyAsync(string iniPath, IReadOnlyDictionary<string, string> values);
}

public sealed class CitizenFxConfigWriter : ICitizenFxConfigWriter
{
    private static readonly byte[] Utf16LeBom = [0xFF, 0xFE];
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    private readonly Encoding? _ansiEncoding;

    /// <summary>Uses the system ANSI code page, the one FiveM's profile API writes in.</summary>
    public CitizenFxConfigWriter()
    {
    }

    /// <summary>Test seam: the code page values are written in for a file without a BOM.</summary>
    public CitizenFxConfigWriter(Encoding ansiEncoding)
    {
        _ansiEncoding = ansiEncoding;
    }

    public async Task ApplyAsync(string iniPath, IReadOnlyDictionary<string, string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);
        ArgumentNullException.ThrowIfNull(values);

        // A key or value carrying a line break could inject arbitrary keys/sections
        // into FiveM's config; unsafe entries are skipped, never written.
        var safeValues = FilterSafeValues(values);

        if (safeValues.Count == 0)
        {
            return;
        }

        var directory = Path.GetDirectoryName(iniPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var bytes = File.Exists(iniPath)
            ? await File.ReadAllBytesAsync(iniPath)
            : [];
        var codec = IniCodec.For(bytes, _ansiEncoding ?? SystemAnsiEncoding());
        var lines = SplitLines(codec.Decode(bytes));
        var fileValues = safeValues.ToDictionary(entry => entry.Key, entry => codec.ToFileText(entry.Value));

        var gameStart = FindGameSectionStart(lines);
        var gameEnd = gameStart is int start
            ? FindNextSectionStart(lines, start + 1)
            : lines.Length;

        var hasChange = false;
        foreach (var (key, value) in fileValues)
        {
            hasChange |= !HasMatchingValue(lines, gameStart, gameEnd, key, value);
        }

        if (!hasChange)
        {
            return;
        }

        var output = BuildOutput(lines, gameStart, gameEnd, fileValues);
        AtomicFile.WriteAllBytes(iniPath, codec.Encode(string.Join(Environment.NewLine, output)));
    }

    private static Encoding SystemAnsiEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(0);
    }

    private static string[] SplitLines(string text)
    {
        var lines = new List<string>();
        using var reader = new StringReader(text);

        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return [.. lines];
    }

    /// <summary>
    /// How the file's bytes map to text. FiveM writes CitizenFX.ini through the Win32
    /// profile API: ANSI code page without a BOM, UTF-16LE with one. Byte-oriented files
    /// are handled as Latin-1 — a byte↔char bijection, so every line the writer does not
    /// change round-trips byte-exact whatever code page wrote it — and only the values the
    /// launcher writes are encoded (ANSI, or UTF-8 behind a UTF-8 BOM) into that domain.
    /// </summary>
    private sealed record IniCodec(byte[] Preamble, Encoding TextEncoding, Encoding? ValueEncoding)
    {
        public static IniCodec For(byte[] bytes, Encoding ansi)
        {
            if (bytes.AsSpan().StartsWith(Utf16LeBom))
            {
                return new IniCodec(Utf16LeBom, Encoding.Unicode, ValueEncoding: null);
            }

            return bytes.AsSpan().StartsWith(Utf8Bom)
                ? new IniCodec(Utf8Bom, Encoding.Latin1, Encoding.UTF8)
                : new IniCodec([], Encoding.Latin1, ansi);
        }

        public string Decode(byte[] bytes)
        {
            return TextEncoding.GetString(bytes, Preamble.Length, bytes.Length - Preamble.Length);
        }

        public string ToFileText(string value)
        {
            return ValueEncoding is null ? value : TextEncoding.GetString(ValueEncoding.GetBytes(value));
        }

        public byte[] Encode(string text)
        {
            return [.. Preamble, .. TextEncoding.GetBytes(text)];
        }
    }

    private static Dictionary<string, string> FilterSafeValues(IReadOnlyDictionary<string, string> values)
    {
        var safe = new Dictionary<string, string>();

        foreach (var (key, value) in values)
        {
            if (!ContainsLineBreak(key) && !ContainsLineBreak(value))
            {
                safe[key] = value;
            }
        }

        return safe;
    }

    private static bool ContainsLineBreak(string text)
    {
        return text.Contains('\r') || text.Contains('\n');
    }

    private static bool HasMatchingValue(
        string[] lines,
        int? gameStart,
        int gameEnd,
        string key,
        string expectedValue)
    {
        if (gameStart is not int start)
        {
            return false;
        }

        for (var i = start + 1; i < gameEnd; i++)
        {
            if (TrySplitKeyValue(lines[i], out var lineKey, out var lineValue) &&
                string.Equals(lineKey, key, StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(lineValue, expectedValue, StringComparison.Ordinal);
            }
        }

        return false;
    }

    private static List<string> BuildOutput(
        string[] lines,
        int? gameStart,
        int gameEnd,
        IReadOnlyDictionary<string, string> values)
    {
        var output = lines.ToList();

        if (gameStart is not int start)
        {
            output.Add("[Game]");
            foreach (var (key, value) in values)
            {
                output.Add($"{key}={value}");
            }

            return output;
        }

        var missing = new List<string>();
        var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = start + 1; i < gameEnd; i++)
        {
            if (!TrySplitKeyValue(lines[i], out var lineKey, out _))
            {
                continue;
            }

            var matchKey = values.Keys.FirstOrDefault(
                key => string.Equals(key, lineKey, StringComparison.OrdinalIgnoreCase));

            if (matchKey is not null)
            {
                var equalsIndex = lines[i].IndexOf('=');
                output[i] = lines[i][..(equalsIndex + 1)] + values[matchKey];
                handled.Add(matchKey);
            }
        }

        missing.AddRange(values.Keys.Where(key => !handled.Contains(key)));

        for (var i = missing.Count - 1; i >= 0; i--)
        {
            output.Insert(gameEnd, $"{missing[i]}={values[missing[i]]}");
        }

        return output;
    }

    private static int? FindGameSectionStart(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            if (string.Equals(lines[i].Trim(), "[Game]", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return null;
    }

    private static int FindNextSectionStart(string[] lines, int fromIndex)
    {
        for (var i = fromIndex; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                return i;
            }
        }

        return lines.Length;
    }

    private static bool TrySplitKeyValue(string line, out string key, out string value)
    {
        var equalsIndex = line.IndexOf('=');
        if (equalsIndex <= 0)
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = line[..equalsIndex].Trim();
        value = line[(equalsIndex + 1)..].Trim();
        return true;
    }
}