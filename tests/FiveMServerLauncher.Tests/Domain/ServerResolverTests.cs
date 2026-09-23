using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Service;
using FiveMServerLauncher.Tests.Service;
namespace FiveMServerLauncher.Tests.Domain;

public class ServerResolverTests
{
    [Fact]
    public async Task Resolve_WhenValidCfxId_ShouldReturnCfxId()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(cfxId);

        // Then
        Assert.Equal(cfxId, result.CfxId);
    }
    [Fact]
    public async Task Resolve_WhenInvalidAddress_ShouldThrow()
    {
        // Given
        const string address = "unknown";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.NotFound,
            string.Empty);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When / Then
        await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address));
    }
    [Fact]
    public async Task Resolve_WhenEmptyAddress_ShouldThrow()
    {
        // Given
        const string address = "   ";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When / Then
        await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address));
    }
    [Fact]
    public async Task Resolve_WhenCfxUrl_ShouldUseCfxIdFromUrl()
    {
        // Given
        const string address = "https://cfx.re/join/y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        await resolver.ResolveAsync(address);

        // Then
        Assert.Equal(
            "https://frontend.cfx-services.net/api/servers/single/y4lg95",
            handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task Resolve_WhenValidCfxUrl_ShouldReturnCfxId()
    {
        // Given
        const string address = "https://cfx.re/join/y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.Equal("y4lg95", result.CfxId);
    }
    [Fact]
    public async Task Resolve_ShouldReturnCfxIdForCfxJoinUrlWithoutScheme()
    {
        // Given
        const string address = "cfx.re/join/y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.Equal(
            "https://frontend.cfx-services.net/api/servers/single/y4lg95",
            handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task Resolve_ShouldReturnProfileWithProjectName()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(cfxId);

        // Then
        Assert.Equal("Test Server", result.ProjectName);
    }

    [Fact]
    public async Task Resolve_ShouldReturnDerivedRequirements()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server",
                    "requestSteamTicket": "on",
                    "vars": {
                        "sv_enforceGameBuild": "3258",
                        "sv_pureLevel": "1"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(cfxId);

        // Then
        Assert.Equal(3258, result.Requirements.GameBuild);
        Assert.Equal(1, result.Requirements.PureMode);
        Assert.True(result.Requirements.RequestSteamTicket);
    }

    [Fact]
    public async Task Resolve_ShouldReturnGameClient()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "gamename": "gta5enhanced"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(cfxId);

        // Then
        Assert.Equal(Core.Enums.GameClient.FiveMEnhanced, result.GameClient);
    }

    [Fact]
    public async Task Resolve_WhenGameClientNotPublished_ShouldReturnNull()
    {
        // Given
        const string cfxId = "y4lg95";

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server"
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var cfxService = new CfxService(httpClient);
        var resolver = CreateResolver(cfxService);

        // When
        var result = await resolver.ResolveAsync(cfxId);

        // Then
        Assert.Null(result.GameClient);
    }

    [Fact]
    public async Task Resolve_WhenIpPortFoundInCatalog_ShouldReturnValidatedProfile()
    {
        // Given
        const string address = "149.56.120.52:30320";

        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars =
                {
                    ["sv_projectName"] = "Catalog Server",
                    ["gamename"] = "gta5",
                    ["sv_enforceGameBuild"] = "3095",
                    ["sv_pureLevel"] = "2",
                    ["requestSteamTicket"] = "on",
                    ["sv_enforceSteamAuth"] = "true"
                },
                ConnectEndPoints = { address }
            }
        };
        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            TestProtobufFrames.BuildFrameStream(protoServer));
        using var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(catalogHttpClient: httpClient);

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal("y4lg95", result.CfxId);
        Assert.Equal("Catalog Server", result.ProjectName);
        Assert.Equal(Core.Enums.GameClient.FiveM, result.GameClient);
        Assert.Equal(3095, result.Requirements.GameBuild);
        Assert.Equal(2, result.Requirements.PureMode);
        Assert.True(result.Requirements.RequestSteamTicket);
        Assert.True(result.Requirements.SteamRequired);
    }

    [Fact]
    public async Task Resolve_WhenIpPortNotInCatalog_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "149.56.120.52:30320";

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "other" }))));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(string.Empty, result.CfxId);
        Assert.Equal(string.Empty, result.ProjectName);
        Assert.Null(result.GameClient);
        Assert.Null(result.Requirements.GameBuild);
        Assert.Null(result.Requirements.PureMode);
        Assert.Null(result.Requirements.RequestSteamTicket);
        Assert.Null(result.Requirements.SteamRequired);
    }

    [Fact]
    public async Task Resolve_WhenDomainPortDnsResolvesAndFound_ShouldReturnValidatedProfile()
    {
        // Given
        const string address = "play.example.com:30120";

        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars =
                {
                    ["sv_projectName"] = "Catalog Server",
                    ["gamename"] = "gta5"
                },
                ConnectEndPoints = { "149.56.120.52:30120" }
            }
        };
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(protoServer))),
            dnsResolver: new FakeDnsResolver("149.56.120.52"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal("y4lg95", result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenDomainPortDnsFails_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "unknown.example.com:30120";

        var resolver = CreateResolver();

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
    }

    [Fact]
    public async Task Resolve_WhenDomainPortDnsResolvesButNotInCatalog_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "play.example.com:30120";

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "other" }))),
            dnsResolver: new FakeDnsResolver("149.56.120.52"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
    }

    [Fact]
    public async Task Resolve_WhenInvalidForm_ShouldThrow()
    {
        // Given
        const string address = "not a valid form";

        var resolver = CreateResolver();

        // When / Then
        await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address));
    }

    [Fact]
    public async Task Resolve_WhenIpPortAndCatalogUnavailable_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "149.56.120.52:30320";

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(true)));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
    }

    [Fact]
    public async Task Resolve_WhenCfxUrlNotFound_ShouldThrowWithOriginalAddress()
    {
        // Given
        const string address = "https://cfx.re/join/y4lg95";

        var resolver = CreateResolver();

        // When / Then
        var exception = await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address));
        Assert.Contains(address, exception.Message);
    }

    [Fact]
    public async Task Resolve_WhenSavedServerRequiresSteam_ShouldApplyManualToUnvalidatedProfile()
    {
        // Given
        const string address = "149.56.120.52:30320";
        var savedServer = SavedServer.Create("My Server", address, requiresSteam: true);

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "other" }))));

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.True(result.Requirements.SteamRequired);
        Assert.Null(result.Requirements.DiscordRequired);
    }

    [Fact]
    public async Task Resolve_WhenBareIpWithDefaultPortInCatalog_ShouldReturnValidatedProfile()
    {
        // Given
        const string address = "149.56.120.52";

        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars = { ["sv_projectName"] = "Catalog Server", ["gamename"] = "gta5" },
                ConnectEndPoints = { "149.56.120.52:30120" }
            }
        };
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(protoServer))));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal("y4lg95", result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenBareIpNotInCatalog_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "149.56.120.52";

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "other" }))));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(string.Empty, result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenBareDomainMatchesProxyEndpointHost_ShouldReturnValidatedProfileWithoutDns()
    {
        // Given
        const string address = "play.example.com";

        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars = { ["sv_projectName"] = "Proxied Server", ["gamename"] = "gta5" },
                ConnectEndPoints = { "https://play.example.com:443/" }
            }
        };
        var dns = new FakeDnsResolver("104.26.1.1");
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(protoServer))),
            dnsResolver: dns);

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal("y4lg95", result.CfxId);
        Assert.False(dns.WasCalled);
    }

    [Fact]
    public async Task Resolve_WhenBareDomainMatchesViaDnsDefaultPort_ShouldReturnValidatedProfile()
    {
        // Given
        const string address = "direct.example.com";

        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars = { ["sv_projectName"] = "Direct Server" },
                ConnectEndPoints = { "149.56.120.52:30120" }
            }
        };
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(protoServer))),
            dnsResolver: new FakeDnsResolver("149.56.120.52"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal("y4lg95", result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenBareDomainNotInCatalog_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "unknown.example.com";

        var resolver = CreateResolver(dnsResolver: new FakeDnsResolver("149.56.120.52"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(string.Empty, result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenBareDomainHostSharedByMultipleServers_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "proxy.shared.io";

        var first = new Master.Server
        {
            EndPoint = "aaaaaa",
            Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io/" } }
        };
        var second = new Master.Server
        {
            EndPoint = "bbbbbb",
            Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io:443/" } }
        };
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.Join(
                    TestProtobufFrames.BuildFrameStream(first),
                    TestProtobufFrames.BuildFrameStream(second)))),
            dnsResolver: new FakeDnsResolver());

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
    }

    [Fact]
    public async Task Resolve_WhenBareDomainAndCatalogUnavailable_ShouldReturnUnvalidatedProfile()
    {
        // Given
        const string address = "play.example.com";

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(true)),
            dnsResolver: new FakeDnsResolver("149.56.120.52"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
    }

    [Fact]
    public async Task Resolve_WhenBareDomainHostAmbiguousAndDnsWouldMatch_ShouldStayUnvalidated()
    {
        // Given — the shared proxy host is ambiguous; the resolved IP:30120 matches a
        // different server. Ambiguity must not be "resolved" by the DNS fallback.
        const string address = "proxy.shared.io";

        var first = new Master.Server
        {
            EndPoint = "aaaaaa",
            Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io/" } }
        };
        var second = new Master.Server
        {
            EndPoint = "bbbbbb",
            Data = new Master.ServerData { ConnectEndPoints = { "https://proxy.shared.io:443/" } }
        };
        var other = new Master.Server
        {
            EndPoint = "cccccc",
            Data = new Master.ServerData { ConnectEndPoints = { "149.56.120.52:30120" } }
        };
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.Join(
                    TestProtobufFrames.BuildFrameStream(first),
                    TestProtobufFrames.BuildFrameStream(second),
                    TestProtobufFrames.BuildFrameStream(other)))),
            dnsResolver: new FakeDnsResolver("149.56.120.52"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(string.Empty, result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenDomainPortMatchesProxyEndpointHost_ShouldReturnValidatedProfile()
    {
        // Given
        const string address = "play.example.com:30120";

        var protoServer = new Master.Server
        {
            EndPoint = "y4lg95",
            Data = new Master.ServerData
            {
                Vars = { ["sv_projectName"] = "Proxied Server" },
                ConnectEndPoints = { "https://play.example.com:443/" }
            }
        };
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(protoServer))),
            dnsResolver: new FakeDnsResolver("104.26.1.1"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal("y4lg95", result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenSavedServerRequiresDiscord_ShouldApplyManualToValidatedProfile()
    {
        // Given
        const string cfxId = "y4lg95";
        var savedServer = SavedServer.Create("My Server", cfxId, requiresDiscord: true);

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "y4lg95",
                "Data": {
                    "sv_projectName": "Test Server",
                    "vars": {
                        "sv_enforceSteamAuth": "false"
                    }
                }
            }
            """);

        using var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(new CfxService(httpClient));

        // When
        var result = await resolver.ResolveAsync(cfxId, savedServer);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.False(result.Requirements.SteamRequired);
        Assert.True(result.Requirements.DiscordRequired);
    }

    private static ServerResolver CreateResolver(
        CfxService? cfxService = null,
        HttpClient? catalogHttpClient = null,
        FakeDnsResolver? dnsResolver = null)
    {
        var service = cfxService ?? new CfxService(
            new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.NotFound, string.Empty)));

        var catalog = new ServerCatalog(
            catalogHttpClient ?? new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK, Array.Empty<byte>())),
            new FakeTimeProvider(),
            TimeSpan.FromDays(1),
            dnsResolver ?? new FakeDnsResolver());

        return new ServerResolver(service, catalog, new ServerRequirementsResolver(),
            dnsResolver ?? new FakeDnsResolver());
    }

}
