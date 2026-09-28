using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public static class CatalogEndpoint
{
    private const string HiddenSentinelHost = "private-placeholder.cfx.re";

    public static string HostOf(string endpoint)
    {
        var withoutScheme = endpoint;
        var schemeIndex = withoutScheme.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex >= 0)
        {
            withoutScheme = withoutScheme[(schemeIndex + 3)..];
        }

        var pathIndex = withoutScheme.IndexOf('/');
        if (pathIndex >= 0)
        {
            withoutScheme = withoutScheme[..pathIndex];
        }

        var portIndex = withoutScheme.IndexOf(':');
        return portIndex >= 0 ? withoutScheme[..portIndex] : withoutScheme;
    }

    public static bool IsHidden(string endpoint)
    {
        var host = HostOf(endpoint);

        return host.Equals(HiddenSentinelHost, StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }
}
