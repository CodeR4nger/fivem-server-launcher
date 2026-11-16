namespace FiveMServerLauncher.Domain;

public sealed record GameBuildOption(int? Build, string Label)
{
    // Reads as its label wherever it is shown or announced as text (a bound list item's accessible name).
    public override string ToString() => Label;
}
