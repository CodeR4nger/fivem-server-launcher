using System.IO;

namespace FiveMServerLauncher.Configuration;

// A store refused a write because its file could not be read: the content on disk is
// unknown, so replacing it could destroy data. Derives from IOException so every caller
// that already treats an IO fault as "could not save" handles it unchanged.
public sealed class StoreUnavailableException : IOException
{
    public StoreUnavailableException(string path)
        : base($"The store file '{path}' could not be read; the write was refused to protect its content.")
    {
    }
}
