using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

public class SavedServerItemTests
{
    [Fact]
    public void Ctor_ShouldExposeSavedServerValues()
    {
        // Given / When
        var item = new SavedServerItem("My Server", "abc123", requiresSteam: true, requiresDiscord: false, () => { });

        // Then
        Assert.Equal("My Server", item.Name);
        Assert.Equal("abc123", item.Address);
        Assert.True(item.RequiresSteam);
        Assert.False(item.RequiresDiscord);
        Assert.Null(item.CfxId);
        Assert.False(item.HasCfxId);
    }

    [Fact]
    public void Ctor_WithoutCfxId_ShouldShowUnresolvedStatus()
    {
        // Given / When
        var item = new SavedServerItem("My Server", "not-published.example.com:30120", null, null, () => { });

        // Then
        Assert.Equal("UNRESOLVED", item.StatusLabel);
        Assert.Null(item.Game);
        Assert.False(item.HasGame);
        Assert.Equal(string.Empty, item.GameTagLabel);
    }

    [Fact]
    public void Ctor_WithCfxId_ShouldExposeIt()
    {
        // Given / When
        var item = new SavedServerItem("My Server", "192.168.1.10:30120", null, null, () => { }, cfxId: "y4lg95");

        // Then
        Assert.Equal("y4lg95", item.CfxId);
        Assert.True(item.HasCfxId);
        Assert.Equal("OFFLINE", item.StatusLabel);
    }

    [Fact]
    public void ApplyPresence_WhenOnline_ShouldSetOnlinePlayersMaxAndGame()
    {
        // Given
        var notifications = new List<string?>();
        var item = new SavedServerItem("My Server", "abc123", null, null, () => { }, cfxId: "y4lg95");
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        item.ApplyPresence(new ServerPresence(true, 12, 64, GameClient.FiveM));

        // Then
        Assert.True(item.Online);
        Assert.Equal(12, item.Players);
        Assert.Equal(64, item.MaxPlayers);
        Assert.Equal("12/64", item.StatusLabel);
        Assert.Equal(GameClient.FiveM, item.Game);
        Assert.True(item.HasGame);
        Assert.Equal("FiveM", item.GameTagLabel);
        Assert.Contains(nameof(SavedServerItem.Online), notifications);
        Assert.Contains(nameof(SavedServerItem.StatusLabel), notifications);
        Assert.Contains(nameof(SavedServerItem.Game), notifications);
        Assert.Contains(nameof(SavedServerItem.GameTagLabel), notifications);
    }

    [Fact]
    public void ApplyPresence_WhenRedM_ShouldExposeRedMTag()
    {
        // Given
        var item = new SavedServerItem("My Server", "abc123", null, null, () => { }, cfxId: "boya5d");
        item.ApplyPresence(new ServerPresence(true, 2, 48, GameClient.RedM));

        // When
        var label = item.GameTagLabel;

        // Then
        Assert.Equal("RedM", label);
    }

    [Fact]
    public void ApplyPresence_WhenOffline_ShouldShowOfflineStatus()
    {
        // Given
        var item = new SavedServerItem("My Server", "abc123", null, null, () => { }, cfxId: "y4lg95");
        item.ApplyPresence(new ServerPresence(true, 12, 64, GameClient.FiveM));

        // When
        item.ApplyPresence(new ServerPresence(false, 0, 0, null));

        // Then
        Assert.False(item.Online);
        Assert.Null(item.Players);
        Assert.Null(item.MaxPlayers);
        Assert.Equal("OFFLINE", item.StatusLabel);
        Assert.Equal(GameClient.FiveM, item.Game);
    }

    [Fact]
    public void ApplyPresence_ShouldNotTriggerChangeHandler()
    {
        // Given
        var handlerRuns = 0;
        var item = new SavedServerItem("My Server", "abc123", null, null, () => handlerRuns++, cfxId: "y4lg95");

        // When
        item.ApplyPresence(new ServerPresence(true, 1, 32, GameClient.FiveM));

        // Then
        Assert.Equal(0, handlerRuns);
    }

    [Fact]
    public void SetIcon_ShouldExposeIconBytesAndNotify()
    {
        // Given
        byte[]? icon = [1, 2, 3];
        var notifications = new List<string?>();
        var item = new SavedServerItem("My Server", "abc123", null, null, () => { }, cfxId: "y4lg95");
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        item.SetIcon(icon);

        // Then
        Assert.Same(icon, item.Icon);
        Assert.True(item.HasIcon);
        Assert.Contains(nameof(SavedServerItem.Icon), notifications);
    }

    [Fact]
    public void SetCfxId_ShouldFlipStatusFromUnresolvedToOffline()
    {
        // Given
        var notifications = new List<string?>();
        var item = new SavedServerItem("My Server", "not-published.example.com:30120", null, null, () => { });
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        item.SetCfxId("y4lg95");

        // Then
        Assert.Equal("OFFLINE", item.StatusLabel);
        Assert.Contains(nameof(SavedServerItem.StatusLabel), notifications);
    }

    [Fact]
    public void RequiresSteam_WhenChanged_ShouldNotifyAndRunHandler()
    {
        // Given
        var handlerRuns = 0;
        var item = new SavedServerItem("My Server", "abc123", null, null, () => handlerRuns++);
        var notifications = 0;
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SavedServerItem.RequiresSteam))
            {
                notifications++;
            }
        };

        // When
        item.RequiresSteam = true;

        // Then
        Assert.True(item.RequiresSteam);
        Assert.Equal(1, notifications);
        Assert.Equal(1, handlerRuns);
    }

    [Fact]
    public void RequiresSteam_WhenSetToSameValue_ShouldNotNotifyNorRunHandler()
    {
        // Given
        var handlerRuns = 0;
        var item = new SavedServerItem("My Server", "abc123", true, null, () => handlerRuns++);

        // When
        item.RequiresSteam = true;

        // Then
        Assert.Equal(0, handlerRuns);
    }

    [Fact]
    public void RequiresDiscord_WhenChanged_ShouldNotifyAndRunHandler()
    {
        // Given
        var handlerRuns = 0;
        var item = new SavedServerItem("My Server", "abc123", null, null, () => handlerRuns++);

        // When
        item.RequiresDiscord = true;

        // Then
        Assert.True(item.RequiresDiscord);
        Assert.Equal(1, handlerRuns);
    }
}