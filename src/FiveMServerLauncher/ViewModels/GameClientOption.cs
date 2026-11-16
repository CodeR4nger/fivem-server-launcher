using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.ViewModels;

public sealed record GameClientOption(GameClient? Game, string Label)
{
    // The item's UI Automation name: WPF falls back to ToString() for a bound item.
    public override string ToString() => Label;
}
