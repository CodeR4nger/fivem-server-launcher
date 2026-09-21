using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Tests.Domain;

public class ServerRequirementsTests
{
    [Fact]
    public void ForConnection_WhenSteamPublishedTrue_ShouldWinOverManualFalse()
    {
        // Given
        var published = new ServerRequirements { SteamRequired = true };
        var savedServer = SavedServer.Create("My Server", "abc123", requiresSteam: false);

        // When
        var result = ServerRequirements.ForConnection(published, savedServer);

        // Then
        Assert.True(result.SteamRequired);
    }

    [Fact]
    public void ForConnection_WhenSteamPublishedFalse_ShouldWinOverManualTrue()
    {
        // Given
        var published = new ServerRequirements { SteamRequired = false };
        var savedServer = SavedServer.Create("My Server", "abc123", requiresSteam: true);

        // When
        var result = ServerRequirements.ForConnection(published, savedServer);

        // Then
        Assert.False(result.SteamRequired);
    }

    [Fact]
    public void ForConnection_WhenSteamNotPublished_ShouldUseManual()
    {
        // Given
        var published = new ServerRequirements();
        var savedServer = SavedServer.Create("My Server", "abc123", requiresSteam: true);

        // When
        var result = ServerRequirements.ForConnection(published, savedServer);

        // Then
        Assert.True(result.SteamRequired);
    }

    [Fact]
    public void ForConnection_ShouldApplyManualDiscordRequired()
    {
        // Given
        var published = new ServerRequirements();
        var savedServer = SavedServer.Create("My Server", "abc123", requiresDiscord: true);

        // When
        var result = ServerRequirements.ForConnection(published, savedServer);

        // Then
        Assert.True(result.DiscordRequired);
    }

    [Fact]
    public void ForConnection_WhenNoSavedServer_ShouldKeepPublishedRequirements()
    {
        // Given
        var published = new ServerRequirements
        {
            GameBuild = 3258,
            PureMode = 1,
            SteamRequired = true
        };

        // When
        var result = ServerRequirements.ForConnection(published, savedServer: null);

        // Then
        Assert.Equal(3258, result.GameBuild);
        Assert.Equal(1, result.PureMode);
        Assert.True(result.SteamRequired);
        Assert.Null(result.DiscordRequired);
    }

    [Fact]
    public void ForConnection_WhenSteamPublishedFalseAndNoManual_ShouldRemainFalse()
    {
        // Given
        var published = new ServerRequirements { SteamRequired = false };
        var savedServer = SavedServer.Create("My Server", "abc123");

        // When
        var result = ServerRequirements.ForConnection(published, savedServer);

        // Then
        Assert.False(result.SteamRequired);
    }

    [Fact]
    public void ForConnection_WhenNothingConfigured_ShouldLeaveRequirementsNull()
    {
        // Given
        var published = new ServerRequirements();
        var savedServer = SavedServer.Create("My Server", "abc123");

        // When
        var result = ServerRequirements.ForConnection(published, savedServer);

        // Then
        Assert.Null(result.SteamRequired);
        Assert.Null(result.DiscordRequired);
    }
}