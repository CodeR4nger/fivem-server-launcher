namespace FiveMServerLauncher.ViewModels;

public sealed record LanguageSettingOption(string? Tag, string Label)
{
    // The item's UI Automation name: WPF falls back to ToString() for a bound item.
    public override string ToString() => Label;
}
