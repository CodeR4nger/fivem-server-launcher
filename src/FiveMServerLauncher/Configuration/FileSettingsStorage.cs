using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;


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

        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, SerializerOptions);

        File.WriteAllText(_filePath, json);
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
