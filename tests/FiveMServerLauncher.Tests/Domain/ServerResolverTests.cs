using FiveMServerLauncher.Core.Enums;
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
        Assert.Equal(GameClient.FiveMEnhanced, result.GameClient);
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
        Assert.Equal(GameClient.FiveM, result.GameClient);
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

    [Fact]
    public async Task Resolve_WhenBareLocalhostNotInCatalog_ShouldReturnUnvalidatedProfile()
    {
        // Given — an unlisted local dev server: DNS resolves the loopback host but the
        // catalog has no matching endpoint, so the profile stays unvalidated as-is.
        const string address = "localhost";

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "other" }))),
            dnsResolver: new FakeDnsResolver("127.0.0.1"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(string.Empty, result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenBareLocalhostMatchesOnlyLoopbackCatalogEndpoint_ShouldStayUnvalidated()
    {
        // Given — the only catalog entry behind the loopback IP is a hidden stranger's
        // server; a typed localhost address must stay an unvalidated direct connect.
        const string address = "localhost";

        var protoServer = new Master.Server
        {
            EndPoint = "8y6354",
            Data = new Master.ServerData
            {
                Vars = { ["sv_projectName"] = "Hidden Dev Server" },
                ConnectEndPoints = { "127.0.0.1:30120" }
            }
        };
        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(protoServer))),
            dnsResolver: new FakeDnsResolver("127.0.0.1"));

        // When
        var result = await resolver.ResolveAsync(address);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(string.Empty, result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenCfxIdUnresolvableAndSavedServerHasDirectAddress_ShouldReturnUnvalidatedDirectProfile()
    {
        // Given — the CFX service cannot resolve the id (delisted server or outage) but the
        // saved server links it to a direct connect address.
        const string address = "y4lg95";
        var savedServer = SavedServer.Create("Local Dev", "localhost:30120", cfxId: address);

        var resolver = CreateResolver();

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal("localhost:30120", result.Address);
        Assert.Equal(string.Empty, result.CfxId);
        Assert.Null(result.GameClient);
    }

    [Fact]
    public async Task Resolve_WhenCfxIdUnresolvableAndSavedServerHasCfxFormAddress_ShouldThrow()
    {
        // Given — the saved row itself was stored by id, so no direct address is known.
        const string address = "cfx.re/join/y4lg95";
        var savedServer = SavedServer.Create("By Id", address);

        var resolver = CreateResolver();

        // When / Then
        await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address, savedServer));
    }

    [Fact]
    public async Task Resolve_WhenFallingBackToDirectAddress_ShouldApplySavedManualRequirements()
    {
        // Given
        const string address = "y4lg95";
        var savedServer = SavedServer.Create("Local Dev", "127.0.0.1:30120", requiresSteam: true, cfxId: address);

        var resolver = CreateResolver();

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.True(result.Requirements.SteamRequired);
    }

    [Fact]
    public async Task Resolve_WhenCfxIdResolves_ShouldIgnoreSavedDirectAddress()
    {
        // Given — the validated path always wins over the saved direct address.
        const string address = "y4lg95";
        var savedServer = SavedServer.Create("Local Dev", "127.0.0.1:30120", cfxId: address);

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
        var resolver = CreateResolver(new CfxService(httpClient));

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal("y4lg95", result.CfxId);
    }

    [Fact]
    public async Task Resolve_WhenCfxServiceOutageAndSavedServerHasDirectAddress_ShouldReturnUnvalidatedDirectProfile()
    {
        // Given — a genuine CFX outage (transport failure, not a 404) must also fall back.
        const string address = "y4lg95";
        var savedServer = SavedServer.Create("Local Dev", "localhost:30120", cfxId: address);
        var resolver = CreateResolver(
            cfxService: new CfxService(new HttpClient(new FakeHttpMessageHandler(true))));

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal("localhost:30120", result.Address);
    }

    [Fact]
    public async Task Resolve_WhenCfxServiceOutageAndNoSavedDirectAddress_ShouldThrowInvalidAddress()
    {
        // Given
        const string address = "y4lg95";
        var resolver = CreateResolver(
            cfxService: new CfxService(new HttpClient(new FakeHttpMessageHandler(true))));

        // When / Then
        await Assert.ThrowsAsync<InvalidAddressException>(
            () => resolver.ResolveAsync(address));
    }

    [Fact]
    public async Task Resolve_WhenLoopbackWithResolvableManualCfxId_ShouldTakeRequirementsFromThatServerAndConnectDirect()
    {
        // Given — the manual id links the loopback address to one specific listed server;
        // its published build/pure/game win over the manual values.
        const string address = "localhost:30120";
        var savedServer = SavedServer.Create(
            "Local Dev", address,
            cfxId: "8y6354", gameBuild: 9999, pureMode: 0, gameClient: GameClient.RedM);

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "8y6354",
                "Data": {
                    "sv_projectName": "Local Dev",
                    "vars": {
                        "gamename": "gta5enhanced",
                        "sv_enforceGameBuild": "3095",
                        "sv_pureLevel": "2"
                    }
                }
            }
            """);
        using var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(new CfxService(httpClient));

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(string.Empty, result.CfxId);
        Assert.Equal(GameClient.FiveMEnhanced, result.GameClient);
        Assert.Equal(3095, result.Requirements.GameBuild);
        Assert.Equal(2, result.Requirements.PureMode);
    }

    [Fact]
    public async Task Resolve_WhenLoopbackWithDelistedManualCfxId_ShouldApplyManualOverrides()
    {
        // Given — the id is delisted; the manual values carry the connection.
        const string address = "localhost:30120";
        var savedServer = SavedServer.Create(
            "Local Dev", address,
            cfxId: "8y6354", gameBuild: 3258, pureMode: 1, gameClient: GameClient.RedM);

        var resolver = CreateResolver();

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(GameClient.RedM, result.GameClient);
        Assert.Equal(3258, result.Requirements.GameBuild);
        Assert.Equal(1, result.Requirements.PureMode);
    }

    [Fact]
    public async Task Resolve_WhenLoopbackWithCfxServiceOutage_ShouldApplyManualOverrides()
    {
        // Given — a CFX outage degrades exactly like a delisted id.
        const string address = "127.0.0.1:30120";
        var savedServer = SavedServer.Create(
            "Local Dev", address, gameBuild: 3095, pureMode: 2, gameClient: GameClient.RedM);

        var resolver = CreateResolver(
            cfxService: new CfxService(new HttpClient(new FakeHttpMessageHandler(true))));

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Equal(GameClient.RedM, result.GameClient);
        Assert.Equal(3095, result.Requirements.GameBuild);
        Assert.Equal(2, result.Requirements.PureMode);
    }

    [Fact]
    public async Task Resolve_WhenLoopbackWithoutOverrides_ShouldStayUnvalidatedDirect()
    {
        // Given — a plain loopback row keeps today's bare direct connect.
        const string address = "localhost:30120";
        var savedServer = SavedServer.Create("Local Dev", address);

        var resolver = CreateResolver();

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Equal(address, result.Address);
        Assert.Null(result.Requirements.GameBuild);
        Assert.Null(result.Requirements.PureMode);
        Assert.Null(result.GameClient);
    }

    [Fact]
    public async Task Resolve_WhenLoopbackManualRequiresSteam_ShouldApplyAdditivelyOverPublishedId()
    {
        // Given — the additive Steam rule survives the loopback path.
        const string address = "localhost:30120";
        var savedServer = SavedServer.Create(
            "Local Dev", address, requiresSteam: true,
            cfxId: "8y6354", gameBuild: 3258, gameClient: GameClient.FiveM);

        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            """
            {
                "EndPoint": "8y6354",
                "Data": {
                    "vars": { "sv_enforceSteamAuth": "false" }
                }
            }
            """);
        using var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(new CfxService(httpClient));

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.True(result.IsCfxValidated);
        Assert.True(result.Requirements.SteamRequired);
    }

    [Fact]
    public async Task Resolve_WhenNonLoopbackSavedServerHasOverrides_ShouldIgnoreThem()
    {
        // Given — manual overrides are loopback-only; a normal address ignores them.
        const string address = "149.56.120.52:30320";
        var savedServer = SavedServer.Create(
            "Remote", address, gameBuild: 3258, pureMode: 2, gameClient: GameClient.RedM);

        var resolver = CreateResolver(
            catalogHttpClient: new HttpClient(new FakeHttpMessageHandler(
                System.Net.HttpStatusCode.OK,
                TestProtobufFrames.BuildFrameStream(new Master.Server { EndPoint = "other" }))));

        // When
        var result = await resolver.ResolveAsync(address, savedServer);

        // Then
        Assert.False(result.IsCfxValidated);
        Assert.Null(result.Requirements.GameBuild);
        Assert.Null(result.Requirements.PureMode);
        Assert.Null(result.GameClient);
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
