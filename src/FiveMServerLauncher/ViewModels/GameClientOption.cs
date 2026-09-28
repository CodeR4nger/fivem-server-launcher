using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.ViewModels;

public sealed record GameClientOption(GameClient? Game, string Label)
{
    public static readonly IReadOnlyList<GameClientOption> DialogOptions =
    [
        new(null, "None"),
        new(GameClient.FiveM, "FiveM"),
        new(GameClient.FiveMEnhanced, "FiveM Enhanced"),
        new(GameClient.RedM, "RedM")
    ];
}
