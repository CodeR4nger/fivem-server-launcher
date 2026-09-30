using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using Xunit;

namespace FiveMServerLauncher.Tests.Domain;

public class GameBuildsTests
{
    [Fact]
    public void Options_WithCustomDataAndNoSelection_ShouldLeadWithNoneAndListBuildsNewestFirst()
    {
        // Given
        var data = new GameBuildData(
            [3095, 2060],
            [],
            new Dictionary<int, string> { [3095] = "The Chop Shop" });

        // When
        var options = GameBuilds.Options(GameClient.FiveM, storedBuild: null, noneLabel: "None", data: data);

        // Then
        GameBuildOption[] expected =
        [
            new(null, "None"),
            new(3095, "[3095] The Chop Shop"),
            new(2060, "2060")
        ];
        Assert.Equal(expected, options);
    }

    [Fact]
    public void Options_WithEnhanced_ShouldReturnEmptyList()
    {
        // Given
        var data = new GameBuildData([3095], [], new Dictionary<int, string>());

        // When
        var options = GameBuilds.Options(GameClient.FiveMEnhanced, storedBuild: 3095, noneLabel: "None", data: data);

        // Then
        Assert.Empty(options);
    }

    [Fact]
    public void Options_WithRedM_ShouldListRedMNumbersFromData()
    {
        // Given
        var data = new GameBuildData([3095], [1491, 1311], new Dictionary<int, string>());

        // When
        var options = GameBuilds.Options(GameClient.RedM, storedBuild: null, noneLabel: "None", data: data);

        // Then
        GameBuildOption[] expected =
        [
            new(null, "None"),
            new(1491, "1491"),
            new(1311, "1311")
        ];
        Assert.Equal(expected, options);
    }

    [Fact]
    public void Options_WhenStoredBuildIsUnlisted_ShouldAppendRawEntry()
    {
        // Given
        var data = new GameBuildData([3095], [], new Dictionary<int, string>());

        // When
        var options = GameBuilds.Options(GameClient.FiveM, storedBuild: 1604, noneLabel: "None", data: data);

        // Then
        GameBuildOption[] expected =
        [
            new(null, "None"),
            new(3095, "3095"),
            new(1604, "1604")
        ];
        Assert.Equal(expected, options);
    }

    [Fact]
    public void Options_WhenStoredBuildIsListed_ShouldNotDuplicateIt()
    {
        // Given
        var data = new GameBuildData([3095], [], new Dictionary<int, string>());

        // When
        var options = GameBuilds.Options(GameClient.FiveM, storedBuild: 3095, noneLabel: "None", data: data);

        // Then
        GameBuildOption[] expected =
        [
            new(null, "None"),
            new(3095, "3095")
        ];
        Assert.Equal(expected, options);
    }

    [Fact]
    public void Options_WhenStoredBuildIsUnlistedButNamed_ShouldShowItsName()
    {
        // Given
        var data = new GameBuildData([3095], [], new Dictionary<int, string> { [2215] = "Cayo Perico Heist" });

        // When
        var options = GameBuilds.Options(GameClient.FiveM, storedBuild: 2215, noneLabel: "None", data: data);

        // Then
        GameBuildOption[] expected =
        [
            new(null, "None"),
            new(3095, "3095"),
            new(2215, "[2215] Cayo Perico Heist")
        ];
        Assert.Equal(expected, options);
    }

    [Fact]
    public void Options_WithoutData_ShouldFallBackToCuratedBaseline()
    {
        // Given / When
        var options = GameBuilds.Options(GameClient.FiveM, storedBuild: null, noneLabel: "None");

        // Then
        Assert.Contains(options, o => o.Build == 3889 && o.Label == "[3889] The Kortz Center Heist");
        Assert.Contains(options, o => o.Build == 1604 && o.Label == "[1604] Arena War");
    }

    [Fact]
    public void Baseline_ShouldMatchCuratedCfxSources()
    {
        // Given: numbers from citizenfx/fivem code/premake5_builds.lua (five/rdr3 sections,
        // newest-first); names from the CFX frontend getGameBuildDLCName mapping with the CFX
        // docs filling 1604/1; RedM builds have no published names.

        // Then
        int[] expectedFiveM =
        [
            3889, 3788, 3751, 3570, 3407, 3323, 3258, 3095, 2944, 2802,
            2699, 2612, 2545, 2372, 2189, 2060, 1604, 1
        ];
        Assert.Equal(expectedFiveM, GameBuilds.Baseline.FiveM);

        int[] expectedRedM = [1491, 1436, 1355, 1311];
        Assert.Equal(expectedRedM, GameBuilds.Baseline.RedM);

        var expectedNames = new Dictionary<int, string>
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
        };
        Assert.Equal(expectedNames, GameBuilds.Baseline.Names);
    }
}
