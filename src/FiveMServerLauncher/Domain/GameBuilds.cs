using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Domain;

public static class GameBuilds
{
    public static GameBuildData Baseline { get; } = new(
    [
        3889, 3788, 3751, 3570, 3407, 3323, 3258, 3095, 2944, 2802,
        2699, 2612, 2545, 2372, 2189, 2060, 1604, 1
    ],
    [1491, 1436, 1355, 1311],
    new Dictionary<int, string>
    {
        [1] = "Base game without any DLCs",
        [1604] = "Arena War",
        [2060] = "Los Santos Summer Special",
        [2189] = "Cayo Perico Heist",
        [2372] = "Los Santos Tuners",
        [2545] = "The Contract",
        [2612] = "The Contract",
        [2699] = "The Criminal Enterprises",
        [2802] = "Los Santos Drug Wars",
        [2944] = "San Andreas Mercenaries",
        [3095] = "The Chop Shop",
        [3258] = "Bottom Dollar Bounties",
        [3323] = "Bottom Dollar Bounties",
        [3407] = "Agents of Sabotage",
        [3570] = "Money Fronts",
        [3751] = "A Safehouse in the Hills",
        [3788] = "A Safehouse in the Hills",
        [3889] = "The Kortz Center Heist"
    });

    public static IReadOnlyList<GameBuildOption> Options(GameClient game, int? storedBuild, string noneLabel, GameBuildData? data = null)
    {
        if (game is not (GameClient.FiveM or GameClient.RedM))
        {
            return [];
        }

        var dataset = data ?? Baseline;
        var numbers = game == GameClient.FiveM ? dataset.FiveM : dataset.RedM;

        var options = new List<GameBuildOption>(numbers.Count + 2) { new(null, noneLabel) };

        foreach (var build in numbers.OrderByDescending(number => number))
        {
            options.Add(new GameBuildOption(build, LabelFor(dataset, build)));
        }

        if (storedBuild is int stored && !numbers.Contains(stored))
        {
            options.Add(new GameBuildOption(stored, LabelFor(dataset, stored)));
        }

        return options;
    }

    private static string LabelFor(GameBuildData dataset, int build) =>
        dataset.Names.TryGetValue(build, out var name) ? $"[{build}] {name}" : build.ToString();
}
