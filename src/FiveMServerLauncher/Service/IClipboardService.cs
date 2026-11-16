namespace FiveMServerLauncher.Service;

// The clipboard seam behind Copy address. Never throws: false when the system
// clipboard refused the text (another process holds it open).
public interface IClipboardService
{
    bool TrySetText(string text);
}
