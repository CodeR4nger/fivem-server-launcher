using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Tests.Service;

internal sealed class FakeClipboardService : IClipboardService
{
    public List<string> Texts { get; } = [];

    public bool Accepts { get; set; } = true;

    public bool TrySetText(string text)
    {
        if (!Accepts)
        {
            return false;
        }

        Texts.Add(text);
        return true;
    }
}
