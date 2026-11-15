using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Localization;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

public class SavedServerItemDetailTests
{
    private static SavedServerItem CreateListedRow(bool? requiresSteam = null, bool? requiresDiscord = null)
    {
        return new SavedServerItem(
            "My Server", "cfx.re/join/y4lg95", requiresSteam, requiresDiscord, () => { }, TestLocalizer.English(), cfxId: "y4lg95");
    }

    private static ServerPresence Online(int? gameBuild = null, int? pureMode = null, bool? steamRequired = null, GameClient? game = GameClient.FiveM)
    {
        return new ServerPresence(true, 12, 64, game, null, gameBuild, pureMode, steamRequired);
    }

    // Requirements: the same cases as ServerRequirementsTests.ForConnection_*.

    [Fact]
    public void EffectiveRequiresSteam_WhenSteamPublishedTrue_ShouldWinOverManualFalse()
    {
        // Given
        var item = CreateListedRow(requiresSteam: false);

        // When
        item.ApplyPresence(Online(steamRequired: true));

        // Then
        Assert.True(item.EffectiveRequiresSteam);
    }

    [Fact]
    public void EffectiveRequiresSteam_WhenSteamPublishedFalseButManualTrue_ShouldRequireSteam()
    {
        // Given
        var item = CreateListedRow(requiresSteam: true);

        // When
        item.ApplyPresence(Online(steamRequired: false));

        // Then
        Assert.True(item.EffectiveRequiresSteam);
    }

    [Fact]
    public void EffectiveRequiresSteam_WhenSteamNotPublished_ShouldUseManual()
    {
        // Given
        var item = CreateListedRow(requiresSteam: true);

        // When
        item.ApplyPresence(Online());

        // Then
        Assert.True(item.EffectiveRequiresSteam);
    }

    [Fact]
    public void EffectiveRequiresDiscord_ShouldApplyManualDiscordRequired()
    {
        // Given
        var item = CreateListedRow(requiresDiscord: true);

        // When
        item.ApplyPresence(Online(steamRequired: false));

        // Then
        Assert.True(item.EffectiveRequiresDiscord);
    }

    [Fact]
    public void EffectiveRequiresSteam_WhenSteamPublishedFalseAndNoManual_ShouldRemainFalse()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online(steamRequired: false));

        // Then
        Assert.False(item.EffectiveRequiresSteam);
    }

    [Fact]
    public void EffectiveRequirements_WhenNothingConfigured_ShouldRequireNothing()
    {
        // Given / When
        var item = CreateListedRow();

        // Then
        Assert.False(item.EffectiveRequiresSteam);
        Assert.False(item.EffectiveRequiresDiscord);
    }

    [Fact]
    public void EffectiveRequiresSteam_WhenServerGoesOffline_ShouldDropTheStalePublishedFlag()
    {
        // Given
        var item = CreateListedRow(requiresSteam: false);
        item.ApplyPresence(Online(steamRequired: true));

        // When
        item.ApplyPresence(ServerPresence.Offline);

        // Then
        Assert.False(item.EffectiveRequiresSteam);
    }

    [Fact]
    public void RequiresSteam_WhenManualFlagChanges_ShouldReEmitEffectiveRequirement()
    {
        // Given
        var item = CreateListedRow();
        var notifications = new List<string?>();
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        item.RequiresSteam = true;
        item.RequiresDiscord = true;

        // Then
        Assert.True(item.EffectiveRequiresSteam);
        Assert.Contains(nameof(SavedServerItem.EffectiveRequiresSteam), notifications);
        Assert.Contains(nameof(SavedServerItem.EffectiveRequiresDiscord), notifications);
    }

    [Fact]
    public void PlayersText_WhenOnline_ShouldShowPlayersOverMax()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online());

        // Then
        Assert.Equal("12/64", item.PlayersText);
    }

    [Fact]
    public void PlayersText_WhenOffline_ShouldBeEmpty()
    {
        // Given
        var item = CreateListedRow();
        item.ApplyPresence(Online());

        // When
        item.ApplyPresence(ServerPresence.Offline);

        // Then
        Assert.Equal(string.Empty, item.PlayersText);
    }

    [Fact]
    public void BuildLabel_WhenNamedBuildPublished_ShouldShowNumberAndDlcName()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online(gameBuild: 3258));

        // Then
        Assert.Equal("3258 · Bottom Dollar Bounties", item.BuildLabel);
    }

    [Fact]
    public void BuildLabel_WhenUnnamedBuildPublished_ShouldShowBareNumber()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online(gameBuild: 1491, game: GameClient.RedM));

        // Then
        Assert.Equal("1491", item.BuildLabel);
    }

    [Fact]
    public void BuildLabel_WhenBuildNotPublished_ShouldShowServerDefault()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online());

        // Then
        Assert.Equal("Server default", item.BuildLabel);
    }

    [Fact]
    public void BuildLabel_WhenSessionNameDataArrives_ShouldUseItAndReEmit()
    {
        // Given
        var item = CreateListedRow();
        item.ApplyPresence(Online(gameBuild: 4000));
        var notifications = new List<string?>();
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
        var data = new GameBuildData([4000], [], new Dictionary<int, string> { [4000] = "Future Heist" });

        // When
        item.SetGameBuildData(data);

        // Then
        Assert.Equal("4000 · Future Heist", item.BuildLabel);
        Assert.Contains(nameof(SavedServerItem.BuildLabel), notifications);
    }

    [Fact]
    public void PureModeLabel_WhenPublished_ShouldShowTheLevel()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online(pureMode: 2));

        // Then
        Assert.Equal("2", item.PureModeLabel);
    }

    [Fact]
    public void PureModeLabel_WhenNotPublished_ShouldShowOff()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online());

        // Then
        Assert.Equal("Off", item.PureModeLabel);
    }

    [Fact]
    public void DetailState_WhenOnline_ShouldBeListedWithNoStateLabel()
    {
        // Given
        var item = CreateListedRow();

        // When
        item.ApplyPresence(Online());

        // Then
        Assert.Equal(ServerDetailState.Listed, item.DetailState);
        Assert.True(item.ShowsDetails);
        Assert.Equal(string.Empty, item.DetailStateLabel);
    }

    [Fact]
    public void DetailState_WhenListedServerGoesOffline_ShouldHideLastKnownValuesBehindOffline()
    {
        // Given
        var item = CreateListedRow();
        item.ApplyPresence(Online(gameBuild: 3258, pureMode: 1));

        // When
        item.ApplyPresence(ServerPresence.Offline);

        // Then
        Assert.Equal(ServerDetailState.Offline, item.DetailState);
        Assert.False(item.ShowsDetails);
        Assert.Equal("Offline", item.DetailStateLabel);
        Assert.Equal("3258 · Bottom Dollar Bounties", item.BuildLabel);
        Assert.Equal("1", item.PureModeLabel);
    }

    [Fact]
    public void DetailState_WhenNoCfxId_ShouldBeNotListed()
    {
        // Given / When
        var item = new SavedServerItem(
            "My Server", "not-published.example.com:30120", null, null, () => { }, TestLocalizer.English());

        // Then
        Assert.Equal(ServerDetailState.NotListed, item.DetailState);
        Assert.False(item.ShowsDetails);
        Assert.Equal("Not listed", item.DetailStateLabel);
        Assert.Equal("Server default", item.BuildLabel);
        Assert.Equal("Off", item.PureModeLabel);
    }

    [Fact]
    public void DetailState_WhenCfxIdIsCapturedLater_ShouldReEmit()
    {
        // Given
        var item = new SavedServerItem(
            "My Server", "192.168.1.10:30120", null, null, () => { }, TestLocalizer.English());
        var notifications = new List<string?>();
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        item.SetCfxId("y4lg95");

        // Then
        Assert.Equal(ServerDetailState.Offline, item.DetailState);
        Assert.Contains(nameof(SavedServerItem.DetailState), notifications);
        Assert.Contains(nameof(SavedServerItem.DetailStateLabel), notifications);
    }

    [Fact]
    public void DetailLabels_WhenLanguageSwitched_ShouldReEmitLocalized()
    {
        // Given
        var localizer = TestLocalizer.For(
            "en-US",
            ("en", """{ "DetailBuildServerDefault": "Server default", "DetailPureModeOff": "Off", "DetailStateNotListed": "Not listed" }"""),
            ("es", """{ "DetailBuildServerDefault": "Predeterminada", "DetailPureModeOff": "Desactivado", "DetailStateNotListed": "No listado" }"""));
        var item = new SavedServerItem("My Server", "10.0.0.1:30120", null, null, () => { }, localizer);
        var notifications = new List<string?>();
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        localizer.SetLanguage("es");

        // Then
        Assert.Equal("Predeterminada", item.BuildLabel);
        Assert.Equal("Desactivado", item.PureModeLabel);
        Assert.Equal("No listado", item.DetailStateLabel);
        Assert.Contains(nameof(SavedServerItem.BuildLabel), notifications);
        Assert.Contains(nameof(SavedServerItem.PureModeLabel), notifications);
        Assert.Contains(nameof(SavedServerItem.DetailStateLabel), notifications);
    }

    private static SavedServerItem CreateManualLoopbackRow(string? cfxId = null)
    {
        return new SavedServerItem(
            "Local", "localhost:30120", null, null, () => { }, TestLocalizer.English(),
            cfxId: cfxId, gameBuild: 3095, pureMode: 1, gameClient: GameClient.FiveM);
    }

    [Fact]
    public void DetailState_WhenLoopbackHasManualParameters_ShouldShowTheManualValues()
    {
        // Given / When
        var item = CreateManualLoopbackRow();

        // Then
        Assert.Equal(ServerDetailState.Manual, item.DetailState);
        Assert.True(item.ShowsDetails);
        Assert.Equal("Manual settings", item.DetailStateLabel);
        Assert.Equal("3095 · The Chop Shop", item.BuildLabel);
        Assert.Equal("1", item.PureModeLabel);
        Assert.Equal(GameClient.FiveM, item.DetailGame);
        Assert.Equal("FiveM", item.DetailGameLabel);
    }

    [Fact]
    public void DetailState_WhenLoopbackManualCfxIdIsDelisted_ShouldShowTheManualValues()
    {
        // Given
        var item = CreateManualLoopbackRow(cfxId: "y4lg95");

        // When
        item.ApplyPresence(ServerPresence.Offline);

        // Then
        Assert.Equal(ServerDetailState.Manual, item.DetailState);
        Assert.Equal("3095 · The Chop Shop", item.BuildLabel);
    }

    [Fact]
    public void DetailState_WhenLoopbackManualCfxIdIsListed_ShouldShowThePublishedValues()
    {
        // Given
        var item = CreateManualLoopbackRow(cfxId: "y4lg95");

        // When
        item.ApplyPresence(Online(gameBuild: 3258, pureMode: null, game: GameClient.RedM));

        // Then
        Assert.Equal(ServerDetailState.Listed, item.DetailState);
        Assert.Equal("3258 · Bottom Dollar Bounties", item.BuildLabel);
        Assert.Equal("Off", item.PureModeLabel);
        Assert.Equal(GameClient.RedM, item.DetailGame);
    }

    [Fact]
    public void DetailState_WhenNonLoopbackCarriesManualParameters_ShouldIgnoreThem()
    {
        // Given / When
        var item = new SavedServerItem(
            "Remote", "10.0.0.1:30120", null, null, () => { }, TestLocalizer.English(),
            gameBuild: 3095, pureMode: 1, gameClient: GameClient.FiveM);

        // Then
        Assert.Equal(ServerDetailState.NotListed, item.DetailState);
        Assert.Equal("Server default", item.BuildLabel);
        Assert.Null(item.DetailGame);
    }
}
