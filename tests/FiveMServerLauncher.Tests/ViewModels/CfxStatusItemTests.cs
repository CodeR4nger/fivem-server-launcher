using System.ComponentModel;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Tests.ViewModels;

public class CfxStatusItemTests
{
    [Theory]
    [InlineData(GameClient.FiveM, "FiveM")]
    [InlineData(GameClient.FiveMEnhanced, "FiveM Enhanced")]
    [InlineData(GameClient.RedM, "RedM")]
    public void Ctor_ShouldExposeClientAndDisplayName(GameClient client, string expectedDisplayName)
    {
        // Given / When
        var item = new CfxStatusItem(client);

        // Then
        Assert.Equal(client, item.Client);
        Assert.Equal(expectedDisplayName, item.DisplayName);
    }

    [Fact]
    public void Ctor_ShouldStartUnknown()
    {
        // Given / When
        var item = new CfxStatusItem(GameClient.FiveM);

        // Then
        Assert.Equal(CfxStatus.Unknown, item.Status);
        Assert.Equal("UNKNOWN", item.StatusLabel);
    }

    [Theory]
    [InlineData(CfxStatus.Operational, "OPERATIONAL")]
    [InlineData(CfxStatus.Degraded, "DEGRADED")]
    [InlineData(CfxStatus.PartialOutage, "PARTIAL OUTAGE")]
    [InlineData(CfxStatus.MajorOutage, "OUTAGE")]
    [InlineData(CfxStatus.Maintenance, "MAINTENANCE")]
    [InlineData(CfxStatus.Unknown, "UNKNOWN")]
    public void Apply_ShouldSetStatusAndStatusLabel(CfxStatus status, string expectedLabel)
    {
        // Given
        var item = new CfxStatusItem(GameClient.FiveM);

        // When
        item.Apply(status);

        // Then
        Assert.Equal(status, item.Status);
        Assert.Equal(expectedLabel, item.StatusLabel);
    }

    [Fact]
    public void Apply_WhenStatusChanges_ShouldNotifyStatusAndStatusLabel()
    {
        // Given
        var notifications = new List<string?>();
        var item = new CfxStatusItem(GameClient.FiveM);
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        item.Apply(CfxStatus.Operational);

        // Then
        Assert.Contains(nameof(CfxStatusItem.Status), notifications);
        Assert.Contains(nameof(CfxStatusItem.StatusLabel), notifications);
    }

    [Fact]
    public void Apply_WhenStatusUnchanged_ShouldNotNotify()
    {
        // Given
        var item = new CfxStatusItem(GameClient.FiveM);
        item.Apply(CfxStatus.Operational);
        var notifications = new List<string?>();
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        // When
        item.Apply(CfxStatus.Operational);

        // Then
        Assert.Empty(notifications);
    }

    [Fact]
    public void Ctor_ShouldImplementNotifyPropertyChanged()
    {
        // Given / When
        var item = new CfxStatusItem(GameClient.FiveM);

        // Then
        Assert.IsAssignableFrom<INotifyPropertyChanged>(item);
    }
}