using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Configuration;

public class FileServerRepository : IServerRepository
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public FileServerRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
    }

    public IReadOnlyList<SavedServer> GetAll()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<SavedServer>();
        }

        var json = File.ReadAllText(_filePath);

        List<SavedServer>? servers;
        try
        {
            servers = JsonSerializer.Deserialize<List<SavedServer>>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            servers = null;
        }

        return servers is null
            ? Array.Empty<SavedServer>()
            : servers
                .Where(s => IsValid(s))
                .ToList();
    }

    private static bool IsValid(SavedServer? savedServer)
    {
        return savedServer is not null
            && !string.IsNullOrWhiteSpace(savedServer.Name)
            && !string.IsNullOrWhiteSpace(savedServer.Address)
            && ServerAddress.Classify(savedServer.Address) != ServerAddressKind.Unknown;
    }

    public void Add(SavedServer savedServer)
    {
        ArgumentNullException.ThrowIfNull(savedServer);

        var servers = GetAll().ToList();

        if (servers.Any(s => s.MatchesAddress(savedServer.Address)))
        {
            throw new ArgumentException($"A server with address '{savedServer.Address}' is already saved.");
        }

        servers.Add(savedServer);
        WriteAll(servers);
    }

    public void Update(SavedServer savedServer)
    {
        ArgumentNullException.ThrowIfNull(savedServer);

        var servers = GetAll().ToList();
        var index = servers.FindIndex(s => s.MatchesAddress(savedServer.Address));

        if (index < 0)
        {
            throw new ArgumentException($"No saved server with address '{savedServer.Address}'.");
        }

        servers[index] = savedServer;
        WriteAll(servers);
    }

    public void Remove(string address)
    {
        var servers = GetAll().ToList();
        var removed = servers.RemoveAll(s => s.MatchesAddress(address));

        if (removed > 0)
        {
            WriteAll(servers);
        }
    }

    private void WriteAll(List<SavedServer> servers)
    {
        var directory = Path.GetDirectoryName(_filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(servers, SerializerOptions));
    }
}