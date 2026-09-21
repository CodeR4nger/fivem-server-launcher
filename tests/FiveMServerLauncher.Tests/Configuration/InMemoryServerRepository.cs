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

    public void Remove(string address)
    {
        _servers.RemoveAll(s => s.MatchesAddress(address));
    }
}