using FiveMServerLauncher.Domain.Exceptions;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Domain;

//TODO: Implement IP and domain filter
public class ServerResolver(CfxService cfxService)
{
    public async Task<ResolvedServer> ResolveAsync(string address)
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

        return new ResolvedServer
        {
            CfxId = server.CfxId
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
