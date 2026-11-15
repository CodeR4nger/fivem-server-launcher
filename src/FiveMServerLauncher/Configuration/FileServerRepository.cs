using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiveMServerLauncher.Core;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Configuration;

// The saved-servers list lives in memory after the first successful read (the app is
// single-instance). Every mutator builds the next list, writes it, and only then commits it,
// so a failed write leaves memory equal to disk. A file that could not be read is never
// overwritten: reads answer empty and mutators throw StoreUnavailableException until a
// retry succeeds. One lock guards all of it — the background cfx-id capture writes from a
// continuation off the caller's thread.
public class FileServerRepository : IServerRepository
{
    private readonly string _filePath;
    private readonly Func<string, string> _readAllText;
    private readonly object _gate = new();

    // Null until a read succeeds (missing, parsed, or corrupt-and-backed-up).
    private List<SavedServer>? _servers;

    // Entries that are not valid saved servers: never shown, re-emitted verbatim after the
    // valid ones on every write so a rewrite can never drop them.
    private List<JsonElement> _preserved = [];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public FileServerRepository(string filePath, Func<string, string>? readAllText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
        _readAllText = readAllText ?? File.ReadAllText;
    }

    public IReadOnlyList<SavedServer> GetAll()
    {
        lock (_gate)
        {
            return TryLoad() ? _servers!.ToArray() : Array.Empty<SavedServer>();
        }
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

        Mutate(servers =>
        {
            if (servers.Any(s => s.MatchesAddress(savedServer.Address)))
            {
                throw new ArgumentException($"A server with address '{savedServer.Address}' is already saved.");
            }

            servers.Add(savedServer);
            return true;
        });
    }

    public void Update(SavedServer savedServer)
    {
        ArgumentNullException.ThrowIfNull(savedServer);

        Mutate(servers =>
        {
            servers[IndexOf(servers, savedServer.Address)] = savedServer;
            return true;
        });
    }

    public void Replace(string address, SavedServer replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        Mutate(servers =>
        {
            servers[IndexOf(servers, address)] = replacement;
            return true;
        });
    }

    public void Remove(string address)
    {
        Mutate(servers => servers.RemoveAll(s => s.MatchesAddress(address)) > 0);
    }

    public void Move(string address, int newIndex)
    {
        Mutate(servers =>
        {
            var index = IndexOf(servers, address);

            if (newIndex < 0 || newIndex >= servers.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(newIndex));
            }

            if (index == newIndex)
            {
                return false;
            }

            var moved = servers[index];
            servers.RemoveAt(index);
            servers.Insert(newIndex, moved);
            return true;
        });
    }

    private static int IndexOf(List<SavedServer> servers, string address)
    {
        var index = servers.FindIndex(s => s.MatchesAddress(address));

        return index >= 0
            ? index
            : throw new ArgumentException($"No saved server with address '{address}'.");
    }

    // The edit runs on a copy and returns whether it changed anything; only a changed list
    // is written, and only a written list is committed to memory.
    private void Mutate(Func<List<SavedServer>, bool> edit)
    {
        lock (_gate)
        {
            if (!TryLoad())
            {
                throw new StoreUnavailableException(_filePath);
            }

            var next = new List<SavedServer>(_servers!);

            if (!edit(next))
            {
                return;
            }

            AtomicFile.WriteAllText(_filePath, Serialize(next));
            _servers = next;
        }
    }

    private bool TryLoad()
    {
        if (_servers is not null)
        {
            return true;
        }

        var read = StoreFile.Read(_filePath, _readAllText);

        switch (read.Kind)
        {
            case StoreReadKind.Missing:
                _servers = [];
                return true;
            case StoreReadKind.Unreadable:
                return false;
        }

        if (TryParse(read.Text, out var servers, out var preserved))
        {
            _servers = servers;
            _preserved = preserved;
            return true;
        }

        if (!StoreFile.TryBackUpCorrupt(_filePath))
        {
            return false;
        }

        _servers = [];
        _preserved = [];
        return true;
    }

    private static bool TryParse(string json, out List<SavedServer> servers, out List<JsonElement> preserved)
    {
        servers = [];
        preserved = [];

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(json);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return false;
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var element in root.EnumerateArray())
        {
            SavedServer? server;
            try
            {
                server = element.Deserialize<SavedServer>(SerializerOptions);
            }
            catch (JsonException)
            {
                server = null;
            }

            if (IsValid(server))
            {
                servers.Add(server!);
            }
            else
            {
                preserved.Add(element);
            }
        }

        return true;
    }

    private static bool IsValid(SavedServer? savedServer)
    {
        return savedServer is not null
            && !string.IsNullOrWhiteSpace(savedServer.Name)
            && !string.IsNullOrWhiteSpace(savedServer.Address)
            && ServerAddress.Classify(savedServer.Address) != ServerAddressKind.Unknown;
    }

    private string Serialize(List<SavedServer> servers)
    {
        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();

            foreach (var server in servers)
            {
                JsonSerializer.Serialize(writer, server, SerializerOptions);
            }

            foreach (var element in _preserved)
            {
                element.WriteTo(writer);
            }

            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
