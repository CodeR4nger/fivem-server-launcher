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