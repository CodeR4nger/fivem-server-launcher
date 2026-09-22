using FiveMServerLauncher.Domain;
using Xunit;

namespace FiveMServerLauncher.Tests.Domain;

public class SavedServerTests
{
    [Fact]
    public void Create_WhenNameIsBlank_ShouldThrowArgumentException()
    {
        // Given / When / Then
        Assert.Throws<ArgumentException>(() => SavedServer.Create("   ", "abc123"));
    }

    [Fact]
    public void Create_WhenAddressIsBlank_ShouldThrowArgumentException()
    {
        // Given / When / Then
        Assert.Throws<ArgumentException>(() => SavedServer.Create("My Server", ""));
    }

    [Theory]
    [InlineData("not a valid one")]
    [InlineData("999.999.5.1:30120")]
    public void Create_WhenAddressNotConnectable_ShouldThrowArgumentException(string address)
    {
        // Given / When / Then
        Assert.Throws<ArgumentException>(() => SavedServer.Create("My Server", address));
    }

    [Fact]
    public void Create_WithCfxIdAddress_ShouldReturnSavedServer()
    {
        // Given
        const string name = "My Favourite Server";
        const string address = "abc123";

        // When
        var result = SavedServer.Create(name, address);

        // Then
        Assert.Equal(name, result.Name);
        Assert.Equal(address, result.Address);
    }

    [Fact]
    public void Create_WithWhitespacePadding_ShouldTrimNameAndAddress()
    {
        // Given
        const string name = "  My Server  ";
        const string address = "  cfx.re/join/abc123  ";

        // When
        var result = SavedServer.Create(name, address);

        // Then
        Assert.Equal("My Server", result.Name);
        Assert.Equal("cfx.re/join/abc123", result.Address);
    }

    [Fact]
    public void Create_WithManualRequirements_ShouldPreserveThem()
    {
        // Given
        const string name = "Strict Server";
        const string address = "play.example.com:30120";

        // When
        var result = SavedServer.Create(name, address, requiresSteam: true, requiresDiscord: false);

        // Then
        Assert.True(result.RequiresSteam);
        Assert.False(result.RequiresDiscord);
    }

    [Fact]
    public void Create_WithoutManualRequirements_ShouldLeaveFlagsUnset()
    {
        // Given / When
        var result = SavedServer.Create("My Server", "abc123");

        // Then
        Assert.Null(result.RequiresSteam);
        Assert.Null(result.RequiresDiscord);
    }

    [Fact]
    public void Create_WithBareCfxIdAddress_ShouldCaptureCfxId()
    {
        // Given
        const string address = "abc123";

        // When
        var result = SavedServer.Create("My Server", address);

        // Then
        Assert.Equal(address, result.CfxId);
    }

    [Fact]
    public void Create_WithCfxJoinUrlAddress_ShouldCaptureExtractedCfxId()
    {
        // Given / When
        var result = SavedServer.Create("My Server", "cfx.re/join/abc123");

        // Then
        Assert.Equal("abc123", result.CfxId);
    }

    [Fact]
    public void Create_WithIpPortAddress_ShouldLeaveCfxIdNull()
    {
        // Given / When
        var result = SavedServer.Create("My Server", "192.168.1.10:30120");

        // Then
        Assert.Null(result.CfxId);
    }

    [Fact]
    public void Create_WithDomainPortAddress_ShouldLeaveCfxIdNull()
    {
        // Given / When
        var result = SavedServer.Create("My Server", "play.example.com:30120");

        // Then
        Assert.Null(result.CfxId);
    }

    [Fact]
    public void Create_WithExplicitCfxId_ShouldPreserveIt()
    {
        // Given / When
        var result = SavedServer.Create("My Server", "192.168.1.10:30120", cfxId: "y4lg95");

        // Then
        Assert.Equal("y4lg95", result.CfxId);
    }

    [Fact]
    public void Create_WithExplicitCfxIdNull_ShouldDeriveFromCfxAddress()
    {
        // Given / When
        var result = SavedServer.Create("My Server", "cfx.re/join/abc123", cfxId: null);

        // Then
        Assert.Equal("abc123", result.CfxId);
    }
}