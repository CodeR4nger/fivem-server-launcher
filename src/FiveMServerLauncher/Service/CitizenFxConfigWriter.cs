using System.IO;

namespace FiveMServerLauncher.Service;

public interface ICitizenFxConfigWriter
{
    Task ApplyAsync(string iniPath, IReadOnlyDictionary<string, string> values);
}

public sealed class CitizenFxConfigWriter : ICitizenFxConfigWriter
{
    public async Task ApplyAsync(string iniPath, IReadOnlyDictionary<string, string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return;
        }

        var directory = Path.GetDirectoryName(iniPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string[] lines = File.Exists(iniPath)
            ? await File.ReadAllLinesAsync(iniPath)
            : [];

        var gameStart = FindGameSectionStart(lines);
        var gameEnd = gameStart is int start
            ? FindNextSectionStart(lines, start + 1)
            : lines.Length;

        var hasChange = false;
        foreach (var (key, value) in values)
        {
            hasChange |= !HasMatchingValue(lines, gameStart, gameEnd, key, value);
        }

        if (!hasChange)
        {
            return;
        }

        var output = BuildOutput(lines, gameStart, gameEnd, values);
        await File.WriteAllTextAsync(iniPath, string.Join(Environment.NewLine, output));
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