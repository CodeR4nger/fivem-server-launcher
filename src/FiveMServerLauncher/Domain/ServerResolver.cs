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

        address = ExtractCfxIdIfValidUrl(address);

        var server = await cfxService.GetServerAsync(address);

        if (server is null)
        {
            throw new InvalidAddressException(address);
        }

        return new ServerProfile
        {
            CfxId = server.CfxId,
            ProjectName = server.ProjectName,
            Requirements = requirementsResolver.Resolve(server)
        };
    }

   private static string ExtractCfxIdIfValidUrl(string address)
    {
        const string prefix = "cfx.re/join/";
        var index = address.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);

        return index >= 0
            ? address[(index + prefix.Length)..]
            : address;
    }


}
