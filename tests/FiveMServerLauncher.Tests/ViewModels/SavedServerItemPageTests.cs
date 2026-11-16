using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Localization;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

// What the Servers page's list row and detail status line say (ui-redesign ticket 06).
public class SavedServerItemPageTests
{
    private static SavedServerItem Row(string address, string? cfxId = null, int? gameBuild = null,
        bool? requiresSteam = null, bool? requiresDiscord = null) =>
        new("My Server", address, requiresSteam, requiresDiscord, () => { }, TestLocalizer.English(),
            cfxId: cfxId, gameBuild: gameBuild);

    private static readonly ServerPresence Live = new(true, 12, 64, GameClient.FiveM);

    [Fact]
    public void ListStatusText_WhenListed_ShouldShowThePlayers()
    {
        // Given
        var row = Row("cfx.re/join/y4lg95", "y4lg95");

        // When
        row.ApplyPresence(Live);

        // Then
        Assert.Equal("12/64", row.ListStatusText);
        Assert.Equal("Online", row.PresenceLabel);
    }

    [Theory]
    [InlineData("cfx.re/join/y4lg95", "y4lg95", null, "Offline")]
    [InlineData("10.0.0.5:30120", null, null, "Not listed")]
    [InlineData("localhost:30120", null, 3258, "Manual settings")]
    public void ListStatusText_WhenNotListed_ShouldSayWhy(string address, string? cfxId, int? build, string expected)
    {
        // Given / When
        var row = Row(address, cfxId, build);

        // Then — the row and the detail status line say the same plain word
        Assert.Equal(expected, row.ListStatusText);
        Assert.Equal(expected, row.PresenceLabel);
    }

    [Fact]
    public void ListStatusText_WhenTheServerGoesOnline_ShouldBeRaised()
    {
        // Given
        var row = Row("cfx.re/join/y4lg95", "y4lg95");
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        row.ApplyPresence(Live);

        // Then
        Assert.Contains(nameof(SavedServerItem.ListStatusText), raised);
        Assert.Contains(nameof(SavedServerItem.PresenceLabel), raised);
        Assert.Contains(nameof(SavedServerItem.HasRequirements), raised);
    }

    [Fact]
    public void HasRequirements_WithNoRequirement_ShouldBeFalse()
    {
        // Given / When
        var row = Row("cfx.re/join/y4lg95", "y4lg95");

        // Then
        Assert.False(row.HasRequirements);
    }

    [Fact]
    public void HasRequirements_WhenTheServerPublishesSteam_ShouldBeTrue()
    {
        // Given
        var row = Row("cfx.re/join/y4lg95", "y4lg95");

        // When
        row.ApplyPresence(Live with { SteamRequired = true });

        // Then
        Assert.True(row.HasRequirements);
    }

    [Fact]
    public void HasRequirements_WhenDiscordIsRequired_ShouldBeTrue()
    {
        // Given / When
        var row = Row("cfx.re/join/y4lg95", "y4lg95", requiresDiscord: true);

        // Then
        Assert.True(row.HasRequirements);
    }

    [Fact]
    public void HasRequirements_WhenTheManualFlagChanges_ShouldBeRaised()
    {
        // Given
        var row = Row("cfx.re/join/y4lg95", "y4lg95");
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        // When
        row.RequiresDiscord = true;

        // Then
        Assert.True(row.HasRequirements);
        Assert.Contains(nameof(SavedServerItem.HasRequirements), raised);
    }
}
