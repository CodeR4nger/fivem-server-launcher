using FiveMServerLauncher.Core;
using Xunit;

namespace FiveMServerLauncher.Tests.Core;

public class ConnectArgumentTests
{
    [Fact]
    public void TryParse_WithConnectFlagAndAddress_ShouldReturnAddress()
    {
        // Given
        var args = new[] { "--connect", "cfx.re/join/y4lg95" };

        // When
        var address = ConnectArgument.TryParse(args);

        // Then
        Assert.Equal("cfx.re/join/y4lg95", address);
    }

    [Fact]
    public void TryParse_WithRepeatedConnectFlag_ShouldReturnLastAddress()
    {
        // Given — the shortcut or the user repeated the flag; the last one wins.
        var args = new[] { "--connect", "127.0.0.1:30120", "--connect", "cfx.re/join/y4lg95" };

        // When
        var address = ConnectArgument.TryParse(args);

        // Then
        Assert.Equal("cfx.re/join/y4lg95", address);
    }

    [Fact]
    public void TryParse_WithUnknownArgsAroundFlag_ShouldIgnoreThem()
    {
        // Given — unknown flags never interfere; zero validation happens here.
        var args = new[] { "--verbose", "--connect", "cfx.re/join/y4lg95", "--nonsense" };

        // When
        var address = ConnectArgument.TryParse(args);

        // Then
        Assert.Equal("cfx.re/join/y4lg95", address);
    }

    public static TheoryData<string[]> NoUsableConnectValueCases => new()
    {
        new string[] { },
        new[] { "--connect" },
        new[] { "--connect", "--connect" },
        new[] { "--verbose", "--dry-run" },
    };

    [Theory]
    [MemberData(nameof(NoUsableConnectValueCases))]
    public void TryParse_WhenNoUsableConnectValue_ShouldReturnNull(string[] args)
    {
        // Given / When
        var address = ConnectArgument.TryParse(args);

        // Then
        Assert.Null(address);
    }
}
