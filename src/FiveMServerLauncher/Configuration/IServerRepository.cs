using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Configuration;

public interface IServerRepository
{
    IReadOnlyList<SavedServer> GetAll();

    SavedServer? FindByAddress(string address);

    SavedServer? FindByCfxId(string cfxId);

    void Add(SavedServer savedServer);

    void Update(SavedServer savedServer);

    // The address-change edit path: the record under `address` is replaced in place
    // by `replacement` (which may carry a new identity), keeping its slot. The edit
    // dialog always means "in place" — it never means "move to the bottom".
    void Replace(string address, SavedServer replacement);

    void Remove(string address);

    // Position-only: the stored record moves to newIndex as it is, so a reorder can
    // never overwrite fields the caller knows nothing about (a cfx id captured in the
    // background, for instance).
    void Move(string address, int newIndex);
}
