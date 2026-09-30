using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Tests.Configuration;

public class InMemoryServerRepository : IServerRepository
{
    private readonly List<SavedServer> _servers = new();

    public IReadOnlyList<SavedServer> GetAll()
    {
        return _servers;
    }

    public SavedServer? FindByAddress(string address)
    {
        return _servers.FirstOrDefault(s => s.MatchesAddress(address));
    }

    public SavedServer? FindByCfxId(string cfxId)
    {
        return _servers.FirstOrDefault(s => s.MatchesCfxId(cfxId));
    }

    public void Add(SavedServer savedServer)
    {
        _servers.Add(savedServer);
    }

    public void Update(SavedServer savedServer)
    {
        var index = _servers.FindIndex(s => s.MatchesAddress(savedServer.Address));

        if (index < 0)
        {
            throw new ArgumentException($"No saved server with address '{savedServer.Address}'.");
        }

        _servers[index] = savedServer;
    }

    public void Replace(string address, SavedServer replacement)
    {
        var index = _servers.FindIndex(s => s.MatchesAddress(address));

        if (index < 0)
        {
            throw new ArgumentException($"No saved server with address '{address}'.");
        }

        _servers[index] = replacement;
    }

    public void Remove(string address)
    {
        _servers.RemoveAll(s => s.MatchesAddress(address));
    }

    public void Move(string address, int newIndex)
    {
        var index = _servers.FindIndex(s => s.MatchesAddress(address));

        if (index < 0)
        {
            throw new ArgumentException($"No saved server with address '{address}'.");
        }

        if (newIndex < 0 || newIndex >= _servers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        }

        if (index == newIndex)
        {
            return;
        }

        var moved = _servers[index];
        _servers.RemoveAt(index);
        _servers.Insert(newIndex, moved);
    }
}
