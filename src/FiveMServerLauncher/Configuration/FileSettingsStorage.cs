using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;
using FiveMServerLauncher.Core;


namespace FiveMServerLauncher.Configuration;

public class FileSettingsStorage : ISettingsStorage
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public FileSettingsStorage(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
    }

    public void Save(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var json = JsonSerializer.Serialize(settings, SerializerOptions);

        AtomicFile.WriteAllText(_filePath, json);
    }
    public LauncherSettings? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        var json = File.ReadAllText(_filePath);

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
