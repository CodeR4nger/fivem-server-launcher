using System.Runtime.InteropServices;
using System.Windows;

namespace FiveMServerLauncher.Service;

// Real clipboard behind Copy address (thin untested glue). WPF retries an open clipboard a
// few times itself; a clipboard another process keeps open still fails with a COM
// ExternalException, which is the "could not copy" answer, never a crash.
public sealed class WpfClipboardService : IClipboardService
{
    public bool TrySetText(string text)
    {
        try
        {
            Clipboard.SetDataObject(text, copy: true);
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }
}
