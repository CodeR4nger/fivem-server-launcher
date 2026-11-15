using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Configuration;

// Load answers null (defaults) for a missing, unreadable or corrupt file, but only the
// missing and backed-up-corrupt cases leave Save open: after an unreadable Load the caller
// holds defaults, not the file's content, so writing them would replace settings nobody
// read. Save stays refused until a later Load succeeds.
public class FileSettingsStorage : ISettingsStorage
{
    private readonly string _filePath;
    private readonly Func<string, string> _readAllText;
    private bool _saveBlocked;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public FileSettingsStorage(string filePath, Func<string, string>? readAllText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
        _readAllText = readAllText ?? File.ReadAllText;
    }

    public void Save(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (_saveBlocked)
        {
            throw new StoreUnavailableException(_filePath);
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);

        AtomicFile.WriteAllText(_filePath, json);
    }

    public LauncherSettings? Load()
    {
        var read = StoreFile.Read(_filePath, _readAllText);

        switch (read.Kind)
        {
            case StoreReadKind.Missing:
                _saveBlocked = false;
                return null;
            case StoreReadKind.Unreadable:
                _saveBlocked = true;
                return null;
        }

        if (TryParse(read.Text) is { } settings)
        {
            _saveBlocked = false;
            return settings;
        }

        _saveBlocked = !StoreFile.TryBackUpCorrupt(_filePath);
        return null;
    }

    private static LauncherSettings? TryParse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<LauncherSettings>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
