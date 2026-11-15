using FiveMServerLauncher.Core;
using Xunit;

namespace FiveMServerLauncher.Tests.Core;

public class LauncherUriTests
{
    public static TheoryData<string, string> AcceptedCases => new()
    {
        // every address form passes through verbatim — the pipeline validates
        { "cfxlauncher://connect/y4lg95", "y4lg95" },
        { "cfxlauncher://connect/cfx.re/join/y4lg95", "cfx.re/join/y4lg95" },
        { "cfxlauncher://connect/127.0.0.1:30120", "127.0.0.1:30120" },
        { "cfxlauncher://connect/play.example.com:30120", "play.example.com:30120" },
        { "cfxlauncher://connect/play.example.com", "play.example.com" },
        { "cfxlauncher://connect/203.0.113.7", "203.0.113.7" },
        // scheme and host are case-insensitive; the address keeps its case
        { "CFXLAUNCHER://CONNECT/AbC123", "AbC123" },
        { "CfxLauncher://Connect/cfx.re/join/AbC", "cfx.re/join/AbC" },
        // percent-decoding happens once
        { "cfxlauncher://connect/cfx.re%2Fjoin%2Fabc", "cfx.re/join/abc" },
        { "cfxlauncher://connect/127.0.0.1%3A30120", "127.0.0.1:30120" },
        { "cfxlauncher://connect/abc%252F", "abc%2F" },
        // a malformed sequence stays literal
        { "cfxlauncher://connect/abc%zz", "abc%zz" },
        // trailing slashes (browsers append one) are stripped
        { "cfxlauncher://connect/abc/", "abc" },
        { "cfxlauncher://connect/cfx.re/join/abc//", "cfx.re/join/abc" },
        // queries and fragments are ignored
        { "cfxlauncher://connect/abc?utm_source=site", "abc" },
        { "cfxlauncher://connect/abc/?x=1#top", "abc" },
        { "cfxlauncher://connect/abc#frag", "abc" },
        // an encoded ? survives into the address (the pipeline rejects it)
        { "cfxlauncher://connect/abc%3Fx", "abc?x" },
        // no validation here: garbage that is not a control character passes through
        { "cfxlauncher://connect/not an address", "not an address" },
    };

    [Theory]
    [MemberData(nameof(AcceptedCases))]
    public void TryGetConnectAddress_WithConnectUri_ShouldReturnDecodedAddress(string uri, string expected)
    {
        // Given / When
        var address = LauncherUri.TryGetConnectAddress(uri);

        // Then
        Assert.Equal(expected, address);
    }

    public static TheoryData<string> RejectedCases => new()
    {
        "",
        "cfxlauncher://",
        "cfxlauncher://connect",
        "cfxlauncher://connect/",
        "cfxlauncher://connect//",
        "cfxlauncher://connect/?x=1",
        "cfxlauncher://connect/%2F",
        "cfxlauncher://open/abc",
        "cfxlauncher://connectx/abc",
        "cfxlauncher://connect.evil/abc",
        "cfxlauncher:connect/abc",
        "cfxlauncher:/connect/abc",
        "fivem://connect/abc",
        "https://connect/abc",
        "xcfxlauncher://connect/abc",
        "--connect",
        // a decoded control character would break the one-line pipe protocol
        "cfxlauncher://connect/abc%0Adef",
        "cfxlauncher://connect/abc%0D",
        "cfxlauncher://connect/abc%00",
        "cfxlauncher://connect/abc\ndef",
    };

    [Theory]
    [MemberData(nameof(RejectedCases))]
    public void TryGetConnectAddress_WithNonConnectUri_ShouldReturnNull(string uri)
    {
        // Given / When
        var address = LauncherUri.TryGetConnectAddress(uri);

        // Then
        Assert.Null(address);
    }
}
