using System.Net;
using System.Net.Sockets;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public sealed class DnsResolver : IDnsResolver
{
    private static readonly TimeSpan ResolveTimeout = TimeSpan.FromSeconds(5);

    public async Task<string?> ResolveToIpAsync(string host)
    {
        try
        {
            using var timeout = new CancellationTokenSource(ResolveTimeout);
            var entry = await Dns.GetHostAddressesAsync(host, timeout.Token);
            return entry.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }
        catch (SocketException)
        {
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}