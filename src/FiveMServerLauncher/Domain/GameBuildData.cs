namespace FiveMServerLauncher.Domain;

public sealed record GameBuildData(
    IReadOnlyList<int> FiveM,
    IReadOnlyList<int> RedM,
    IReadOnlyDictionary<int, string> Names);
