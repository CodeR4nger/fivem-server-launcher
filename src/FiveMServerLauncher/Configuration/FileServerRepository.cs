using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiveMServerLauncher.Core;
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

        List<SavedServer>? servers;
        try
        {
            var json = File.ReadAllText(_filePath);

            servers = JsonSerializer.Deserialize<List<SavedServer>>(json, SerializerOptions);
        }
        catch (IOException)
        {
            // An AV lock or transient IO fault degrades like a JSON fault: to empty.
            servers = null;
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

    public SavedServer? FindByAddress(string address)
    {
        return GetAll().FirstOrDefault(s => s.MatchesAddress(address));
    }

    public SavedServer? FindByCfxId(string cfxId)
    {
        return GetAll().FirstOrDefault(s => s.MatchesCfxId(cfxId));
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
        AtomicFile.WriteAllText(_filePath, JsonSerializer.Serialize(servers, SerializerOptions));
    }
}