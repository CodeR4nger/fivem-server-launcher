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
    public void Classify_WhenBareIpWithoutPort_ShouldReturnIpAddress()
    {
        // Given
        const string address = "149.56.120.52";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.IpAddress, kind);
    }

    [Fact]
    public void Classify_WhenBareDomainWithoutPort_ShouldReturnDomainName()
    {
        // Given
        const string address = "play.example.com";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.DomainName, kind);
    }

    [Fact]
    public void Classify_WhenBareLocalhost_ShouldReturnDomainName()
    {
        // Given
        const string address = "localhost";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.DomainName, kind);
    }

    [Fact]
    public void Classify_WhenBareLocalhostUppercase_ShouldReturnDomainName()
    {
        // Given
        const string address = "LOCALHOST";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.DomainName, kind);
    }

    [Fact]
    public void Classify_WhenBareSingleLabelOtherThanLocalhost_ShouldReturnCfxId()
    {
        // Given
        const string address = "myserver";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.CfxId, kind);
    }

    [Fact]
    public void Classify_WhenBareIpOctetOutOfRange_ShouldReturnUnknown()
    {
        // Given
        const string address = "999.56.120.52";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.Unknown, kind);
    }

    [Fact]
    public void Classify_WhenBareDomainWithInvalidLabel_ShouldReturnUnknown()
    {
        // Given
        const string address = "play.example!.com";

        // When
        var kind = ServerAddress.Classify(address);

        // Then
        Assert.Equal(ServerAddressKind.Unknown, kind);
    }

    [Fact]
    public void Classify_WhenSingleLabelHost_ShouldReturnDomainPort()
    {
        // Given
        const string address = "localhost:30120";

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

    [Theory]
    [InlineData(ServerAddressKind.IpPort)]
    [InlineData(ServerAddressKind.DomainPort)]
    [InlineData(ServerAddressKind.IpAddress)]
    [InlineData(ServerAddressKind.DomainName)]
    public void IsDirectAddress_WhenDirectKind_ShouldReturnTrue(ServerAddressKind kind)
    {
        // Given / When / Then
        Assert.True(ServerAddress.IsDirectAddress(kind));
    }

    [Theory]
    [InlineData(ServerAddressKind.Unknown)]
    [InlineData(ServerAddressKind.CfxId)]
    [InlineData(ServerAddressKind.CfxJoinUrl)]
    public void IsDirectAddress_WhenNonDirectKind_ShouldReturnFalse(ServerAddressKind kind)
    {
        // Given / When / Then
        Assert.False(ServerAddress.IsDirectAddress(kind));
    }

    [Theory]
    [InlineData(ServerAddressKind.CfxId)]
    [InlineData(ServerAddressKind.CfxJoinUrl)]
    public void IsIdForm_WhenIdKind_ShouldReturnTrue(ServerAddressKind kind)
    {
        // Given / When / Then
        Assert.True(ServerAddress.IsIdForm(kind));
    }

    [Theory]
    [InlineData(ServerAddressKind.Unknown)]
    [InlineData(ServerAddressKind.IpPort)]
    [InlineData(ServerAddressKind.DomainPort)]
    [InlineData(ServerAddressKind.IpAddress)]
    [InlineData(ServerAddressKind.DomainName)]
    public void IsIdForm_WhenNonIdKind_ShouldReturnFalse(ServerAddressKind kind)
    {
        // Given / When / Then
        Assert.False(ServerAddress.IsIdForm(kind));
    }

    [Theory]
    [InlineData("ab\"c123")]
    [InlineData("a&b123")]
    [InlineData("a?b123")]
    [InlineData("a#b123")]
    [InlineData("a=b123")]
    [InlineData("a%20b")]
    [InlineData("a/b")]
    [InlineData("a\\b123")]
    [InlineData("a;b")]
    public void Classify_WhenBareIdHasUnsafeCharacters_ShouldReturnUnknown(string address)
    {
        // Given / When / Then — shell/URI metacharacters can never pass as a cfx id.
        Assert.Equal(ServerAddressKind.Unknown, ServerAddress.Classify(address));
    }

    [Theory]
    [InlineData("y4lg95")]
    [InlineData("abc-123")]
    [InlineData("ABC123")]
    public void Classify_WhenBareIdUsesSafeCharset_ShouldReturnCfxId(string address)
    {
        // Given / When / Then
        Assert.Equal(ServerAddressKind.CfxId, ServerAddress.Classify(address));
    }

    [Fact]
    public void Classify_WhenJoinUrlIdHasUnsafeCharacters_ShouldReturnUnknown()
    {
        // Given / When / Then
        Assert.Equal(ServerAddressKind.Unknown, ServerAddress.Classify("cfx.re/join/ab\"cd"));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("localhost:30120")]
    [InlineData("LOCALHOST:30120")]
    [InlineData("127.0.0.1:30120")]
    public void IsLoopbackAddress_WhenLoopbackForm_ShouldReturnTrue(string address)
    {
        // Given / When / Then
        Assert.True(ServerAddress.IsLoopbackAddress(address));
    }

    [Theory]
    [InlineData("192.168.1.10")]
    [InlineData("192.168.1.10:30120")]
    [InlineData("play.example.com")]
    [InlineData("play.example.com:30120")]
    [InlineData("abc123")]
    [InlineData("cfx.re/join/abc123")]
    [InlineData("")]
    public void IsLoopbackAddress_WhenNonLoopbackForm_ShouldReturnFalse(string address)
    {
        // Given / When / Then
        Assert.False(ServerAddress.IsLoopbackAddress(address));
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