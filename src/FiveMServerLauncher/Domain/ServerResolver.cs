using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Service;
using System.Net.Http;

namespace FiveMServerLauncher.Domain;

public class ServerResolver(
    CfxService cfxService,
    ServerCatalog serverCatalog,
    ServerRequirementsResolver requirementsResolver,
    IDnsResolver dnsResolver)
{
    public Task<ServerProfile> ResolveAsync(string address)
    {
        return ResolveAsync(address, savedServer: null);
    }

    public async Task<ServerProfile> ResolveAsync(string address, SavedServer? savedServer)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidAddressException(address);
        }

        var kind = ServerAddress.Classify(address);

        if (ServerAddress.IsLoopbackAddress(address)
            && savedServer is not null
            && HasManualConnectionData(savedServer))
        {
            var loopbackProfile = await ResolveLoopbackAsync(address, savedServer);
            loopbackProfile.Requirements = ServerRequirements.ForConnection(loopbackProfile.Requirements, savedServer);
            return loopbackProfile;
        }

        var profile = kind switch
        {
            ServerAddressKind.CfxId or ServerAddressKind.CfxJoinUrl => await ResolveCfxAsync(address, savedServer),
            ServerAddressKind.IpPort => await ResolveIpPortAsync(address),
            ServerAddressKind.DomainPort => await ResolveDomainPortAsync(address),
            ServerAddressKind.IpAddress => await ResolveBareIpAsync(address),
            ServerAddressKind.DomainName => await ResolveBareDomainAsync(address),
            _ => throw new InvalidAddressException(address)
        };

        profile.Requirements = ServerRequirements.ForConnection(profile.Requirements, savedServer);

        return profile;
    }

    private async Task<ServerProfile> ResolveCfxAsync(string originalAddress, SavedServer? savedServer)
    {
        var cfxId = ServerAddress.ExtractCfxId(originalAddress);

        var server = await TryGetServerAsync(cfxId);

        if (server is null)
        {
            return BuildDirectFallbackProfile(savedServer)
                ?? throw new InvalidAddressException(originalAddress);
        }

        return new ServerProfile
        {
            CfxId = server.CfxId,
            ProjectName = server.ProjectName,
            GameClient = server.GameClient,
            Requirements = requirementsResolver.Resolve(server),
            IsCfxValidated = true
        };
    }

    private static bool HasManualConnectionData(SavedServer savedServer)
    {
        return savedServer.CfxId is not null
            || savedServer.GameBuild.HasValue
            || savedServer.PureMode.HasValue
            || savedServer.GameClient.HasValue;
    }

    private async Task<ServerProfile> ResolveLoopbackAsync(string address, SavedServer savedServer)
    {
        // The manual cfx id links the loopback address to one specific listed server;
        // its published facts win over the manual values whenever it resolves.
        var server = savedServer.CfxId is null ? null : await TryGetServerAsync(savedServer.CfxId);

        if (server is not null)
        {
            return new ServerProfile
            {
                Address = address,
                GameClient = server.GameClient,
                Requirements = requirementsResolver.Resolve(server),
                IsCfxValidated = true
            };
        }

        return new ServerProfile
        {
            Address = address,
            GameClient = savedServer.GameClient,
            Requirements = new ServerRequirements
            {
                GameBuild = savedServer.GameBuild,
                PureMode = savedServer.PureMode
            },
            IsCfxValidated = false
        };
    }

    private async Task<CfxServerInfo?> TryGetServerAsync(string cfxId)
    {
        try
        {
            return await cfxService.GetServerAsync(cfxId);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private static ServerProfile? BuildDirectFallbackProfile(SavedServer? savedServer)
    {
        if (savedServer is null)
        {
            return null;
        }

        return ServerAddress.IsDirectAddress(ServerAddress.Classify(savedServer.Address))
            ? BuildUnvalidatedProfile(savedServer.Address)
            : null;
    }

    private async Task<ServerProfile> ResolveIpPortAsync(string address)
    {
        var server = await serverCatalog.LookupByIpPortAsync(address);

        return server is not null
            ? BuildValidatedProfile(server)
            : BuildUnvalidatedProfile(address);
    }

    private async Task<ServerProfile> ResolveDomainPortAsync(string address)
    {
        ServerAddress.TrySplitHostPort(address, out var host, out var port);

        var byHost = await FindUniqueByHostAsync(host);
        if (byHost is not null)
        {
            return BuildValidatedProfile(byHost);
        }

        var ip = await dnsResolver.ResolveToIpAsync(host);

        if (ip is not null)
        {
            var byIp = await serverCatalog.LookupByIpPortAsync($"{ip}:{port}")
                       ?? await serverCatalog.LookupByIpPortAsync(address);

            if (byIp is not null)
            {
                return BuildValidatedProfile(byIp);
            }
        }

        return BuildUnvalidatedProfile(address);
    }

    private async Task<ServerProfile> ResolveBareIpAsync(string address)
    {
        var server = await serverCatalog.LookupBareIpAsync(address);

        return server is not null
            ? BuildValidatedProfile(server)
            : BuildUnvalidatedProfile(address);
    }

    private async Task<ServerProfile> ResolveBareDomainAsync(string address)
    {
        var server = await serverCatalog.LookupBareDomainAsync(address);

        return server is not null
            ? BuildValidatedProfile(server)
            : BuildUnvalidatedProfile(address);
    }

    private async Task<Master.Server?> FindUniqueByHostAsync(string host)
    {
        var matches = await serverCatalog.FindByEndpointHostAsync(host);

        return matches.Count == 1 ? matches[0] : null;
    }

    private static ServerProfile BuildUnvalidatedProfile(string address)
    {
        return new ServerProfile
        {
            Address = address,
            IsCfxValidated = false
        };
    }

    private static ServerProfile BuildValidatedProfile(Master.Server server)
    {
        var vars = server.Data.Vars;

        return new ServerProfile
        {
            CfxId = server.EndPoint,
            Address = server.Data.ConnectEndPoints.FirstOrDefault(),
            ProjectName = vars.TryGetValue("sv_projectName", out var projectName) ? projectName : server.Data.Hostname,
            GameClient = CfxVars.TryGetGameClient(vars),
            Requirements = BuildRequirements(vars),
            IsCfxValidated = true
        };
    }

    private static ServerRequirements BuildRequirements(IDictionary<string, string> vars)
    {
        return new ServerRequirements
        {
            GameBuild = CfxVars.TryGetInt(vars, "sv_enforceGameBuild"),
            PureMode = CfxVars.TryGetInt(vars, "sv_pureLevel"),
            RequestSteamTicket = CfxVars.MapSteamTicket(
                vars.TryGetValue("requestSteamTicket", out var steam) ? steam : null),
            SteamRequired = CfxVars.TryGetBool(vars, "sv_enforceSteamAuth")
        };
    }
}