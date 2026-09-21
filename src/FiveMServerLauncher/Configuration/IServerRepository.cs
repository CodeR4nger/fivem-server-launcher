using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Configuration;

public interface IServerRepository
{
    IReadOnlyList<SavedServer> GetAll();

    void Add(SavedServer savedServer);

    void Update(SavedServer savedServer);

    void Remove(string address);
}