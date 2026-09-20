using FiveMServerLauncher.Domain;
namespace FiveMServerLauncher.Tests.Domain;

public class ServerAddressTests
{
    [Fact]
    public void Classify_WhenCfxJoinUrlWithoutScheme_ShouldReturnCfxJoinUrl()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.CfxJoinUrl, kind);
    }

    [Fact]
    public void Classify_WhenCfxJoinUrlWithScheme_ShouldReturnCfxJoinUrl()
    {
        // Given
        const string address = "https://cfx.re/join/y4lg95";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.CfxJoinUrl, kind);
    }

    [Fact]
    public void Classify_WhenBareCfxId_ShouldReturnCfxId()
    {
        // Given
        const string address = "8e8xxv";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.CfxId, kind);
    }

    [Fact]
    public void Classify_WhenValidIpPort_ShouldReturnIpPort()
    {
        // Given
        const string address = "149.56.120.52:30320";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.IpPort, kind);
    }

    [Fact]
    public void Classify_WhenIpOctetOutOfRange_ShouldReturnUnknown()
    {
        // Given
        const string address = "999.56.120.52:30320";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.Unknown, kind);
    }

    [Fact]
    public void Classify_WhenPortOutOfRange_ShouldReturnUnknown()
    {
        // Given
        const string address = "149.56.120.52:70000";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.Unknown, kind);
    }

    [Fact]
    public void Classify_WhenValidDomainPort_ShouldReturnDomainPort()
    {
        // Given
        const string address = "play.example.com:30120";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.DomainPort, kind);
    }

    [Fact]
    public void Classify_WhenDomainWithScheme_ShouldReturnUnknown()
    {
        // Given
        const string address = "https://play.example.com:30120";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.Unknown, kind);
    }

    [Fact]
    public void Classify_WhenDomainWithPath_ShouldReturnUnknown()
    {
        // Given
        const string address = "play.example.com:30120/join";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.Unknown, kind);
    }

    [Fact]
    public void Classify_WhenAddressHasWhitespace_ShouldReturnUnknown()
    {
        // Given
        const string address = "cfx.re/join/y4 lg95";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.Unknown, kind);
    }

    [Fact]
    public void HasServerFormWithNonEmptyId_WhenCfxJoinUrlWithoutScheme_ShouldReturnTrue()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";

        // When
        var result = ServerAddress.HasServerFormWithNonEmptyId(address);

        // Then
        Assert.True(result);
    }

    [Fact]
    public void HasServerFormWithNonEmptyId_WhenCfxJoinUrlWithWhitespaceId_ShouldReturnFalse()
    {
        // Given
        const string address = "cfx.re/join/y4 lg95";

        // When
        var result = ServerAddress.HasServerFormWithNonEmptyId(address);

        // Then
        Assert.False(result);
    }

    [Fact]
    public void HasServerFormWithNonEmptyId_WhenBareId_ShouldReturnFalse()
    {
        // Given
        const string address = "y4lg95";

        // When
        var result = ServerAddress.HasServerFormWithNonEmptyId(address);

        // Then
        Assert.False(result);
    }
}