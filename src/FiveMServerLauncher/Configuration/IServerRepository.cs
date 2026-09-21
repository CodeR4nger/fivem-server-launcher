using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Configuration;

public interface IServerRepository
{
    IReadOnlyList<SavedServer> GetAll();

    SavedServer? FindByAddress(string address);

    void Add(SavedServer savedServer);

    void Update(SavedServer savedServer);

    void Remove(string address);
}