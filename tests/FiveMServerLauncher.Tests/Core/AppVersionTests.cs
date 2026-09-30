using FiveMServerLauncher.Core;
using Xunit;

namespace FiveMServerLauncher.Tests.Core;

public class AppVersionTests
{
    [Fact]
    public void TryParseTag_WhenVTaggedThreePartVersion_ShouldParse()
    {
        // Given
        const string tag = "v1.2.3";

        // When
        var version = AppVersion.TryParseTag(tag);

        // Then
        Assert.NotNull(version);
        Assert.Equal(new Version(1, 2, 3), version);
    }

    [Fact]
    public void TryParseTag_WhenBareThreePartVersion_ShouldParse()
    {
        // Given
        const string tag = "1.4.0";

        // When
        var version = AppVersion.TryParseTag(tag);

        // Then
        Assert.NotNull(version);
        Assert.Equal(new Version(1, 4, 0), version);
    }

    [Theory]
    [InlineData("v1.2")]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    [InlineData("vX.Y.Z")]
    [InlineData("v1.2.3-beta")]
    [InlineData(" v1.2.3")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseTag_WhenGarbageOrUntaggedForm_ShouldReturnNull(string? tag)
    {
        // Given / When
        var version = AppVersion.TryParseTag(tag);

        // Then
        Assert.Null(version);
    }

    [Fact]
    public void TryParseTag_WhenAppInfoVersion_ShouldParse()
    {
        // Given / When
        var version = AppVersion.TryParseTag(AppInfo.Version);

        // Then
        Assert.NotNull(version);
    }
}
