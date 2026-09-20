using System.Net;
using System.Net.Sockets;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public sealed class DnsResolver : IDnsResolver
{
    public async Task<string?> ResolveToIpAsync(string host)
    {
        try
        {
            var entry = await Dns.GetHostAddressesAsync(host);
            return entry.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }
        catch (SocketException)
        {
            return null;
        }
    }
}