using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Service;

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

        var lookup = await LookupServerAsync(cfxId);

        if (lookup.Server is not { } server)
        {
            // A saved direct address is the user's own CFX-independent path and wins.
            // Otherwise only an authoritative "not found" is an invalid address; on an
            // outage FiveM can still resolve the id itself through the join form.
            return BuildDirectFallbackProfile(savedServer)
                ?? (lookup.IsOutage
                    ? BuildUnvalidatedProfile(ServerAddress.FromCfxId(cfxId))
                    : throw new InvalidAddressException(originalAddress));
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
        var server = savedServer.CfxId is null ? null : (await LookupServerAsync(savedServer.CfxId)).Server;

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

    private async Task<(CfxServerInfo? Server, bool IsOutage)> LookupServerAsync(string cfxId)
    {
        try
        {
            return (await cfxService.GetServerAsync(cfxId), IsOutage: false);
        }
        catch (Exception exception) when (BoundedContent.IsOutage(exception))
        {
            return (null, IsOutage: true);
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

    private ServerProfile BuildValidatedProfile(Master.Server server)
    {
        var vars = server.Data.Vars;
        var info = CfxVars.ToServerInfo(
            server.EndPoint,
            vars.TryGetValue("sv_projectName", out var projectName) ? projectName : server.Data.Hostname,
            vars,
            CfxVars.TryGetString(vars, "requestSteamTicket"));

        return new ServerProfile
        {
            CfxId = info.CfxId,
            Address = server.Data.ConnectEndPoints.FirstOrDefault(),
            ProjectName = info.ProjectName,
            GameClient = info.GameClient,
            Requirements = requirementsResolver.Resolve(info),
            IsCfxValidated = true
        };
    }
}