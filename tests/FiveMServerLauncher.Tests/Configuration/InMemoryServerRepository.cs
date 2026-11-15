using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Tests.Configuration;

// Mirrors the FileServerRepository contract: snapshot reads, duplicate Add rejected, and a
// mutator that fails to persist throws without changing the store (set WriteFailure to make
// every mutator throw it, as a locked or read-only file would).
public class InMemoryServerRepository : IServerRepository
{
    private readonly object _gate = new();
    private List<SavedServer> _servers = new();

    public Exception? WriteFailure { get; set; }

    public IReadOnlyList<SavedServer> GetAll()
    {
        lock (_gate)
        {
            return _servers.ToArray();
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
        Mutate(servers =>
        {
            if (servers.Any(s => s.MatchesAddress(savedServer.Address)))
            {
                throw new ArgumentException($"A server with address '{savedServer.Address}' is already saved.");
            }

            servers.Add(savedServer);
        });
    }

    public void Update(SavedServer savedServer)
    {
        Mutate(servers => servers[IndexOf(servers, savedServer.Address)] = savedServer);
    }

    public void Replace(string address, SavedServer replacement)
    {
        Mutate(servers => servers[IndexOf(servers, address)] = replacement);
    }

    public void Remove(string address)
    {
        Mutate(servers => servers.RemoveAll(s => s.MatchesAddress(address)));
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

            var moved = servers[index];
            servers.RemoveAt(index);
            servers.Insert(newIndex, moved);
        });
    }

    private static int IndexOf(List<SavedServer> servers, string address)
    {
        var index = servers.FindIndex(s => s.MatchesAddress(address));

        return index >= 0
            ? index
            : throw new ArgumentException($"No saved server with address '{address}'.");
    }

    private void Mutate(Action<List<SavedServer>> edit)
    {
        lock (_gate)
        {
            var next = new List<SavedServer>(_servers);
            edit(next);

            if (WriteFailure is { } failure)
            {
                throw failure;
            }

            _servers = next;
        }
    }
}
