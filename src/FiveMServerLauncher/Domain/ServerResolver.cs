using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Domain;

public class ServerResolver(
    CfxService cfxService,
    ServerCatalog serverCatalog,
    ServerRequirementsResolver requirementsResolver,
    IDnsResolver dnsResolver)
{
    public async Task<ServerProfile> ResolveAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidAddressException(address);
        }

        var kind = ServerAddress.Classify(address);

        return kind switch
        {
            ServerAddressKind.CfxId or ServerAddressKind.CfxJoinUrl => await ResolveCfxAsync(address),
            ServerAddressKind.IpPort => await ResolveIpPortAsync(address),
            ServerAddressKind.DomainPort => await ResolveDomainPortAsync(address),
            _ => throw new InvalidAddressException(address)
        };
    }

    private async Task<ServerProfile> ResolveCfxAsync(string address)
    {
        address = ServerAddress.ExtractCfxId(address);

        var server = await cfxService.GetServerAsync(address);

        if (server is null)
        {
            throw new InvalidAddressException(address);
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

    private async Task<ServerProfile> ResolveIpPortAsync(string address)
    {
        var server = await serverCatalog.LookupByIpPortAsync(address);

        return server is not null
            ? BuildValidatedProfile(server)
            : BuildUnvalidatedProfile(address);
    }

    private async Task<ServerProfile> ResolveDomainPortAsync(string address)
    {
        var ip = await dnsResolver.ResolveToIpAsync(GetHost(address));

        if (ip is null)
        {
            return BuildUnvalidatedProfile(address);
        }

        var server = await serverCatalog.LookupByIpPortAsync($"{ip}:{GetPort(address)}")
                     ?? await serverCatalog.LookupByIpPortAsync(address);

        return server is not null
            ? BuildValidatedProfile(server)
            : BuildUnvalidatedProfile(address);
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
            GameClient = vars.TryGetValue("gamename", out var game) ? CfxVars.MapGameClient(game) : null,
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
                vars.TryGetValue("requestSteamTicket", out var steam) ? steam : null)
        };
    }

    private static string GetHost(string address)
    {
        var separator = address.LastIndexOf(':');
        return address[..separator];
    }

    private static string GetPort(string address)
    {
        var separator = address.LastIndexOf(':');
        return address[(separator + 1)..];
    }
}