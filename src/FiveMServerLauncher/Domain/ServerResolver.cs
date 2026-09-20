using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Domain;

//TODO: Implement IP and domain filter
public class ServerResolver(CfxService cfxService, ServerRequirementsResolver requirementsResolver)
{
    public async Task<ServerProfile> ResolveAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidAddressException(address);
        }

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
            Requirements = requirementsResolver.Resolve(server)
        };
    }
}
