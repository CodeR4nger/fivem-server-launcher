namespace FiveMServerLauncher.Core;

/// <summary>
/// Owns the <c>cfxlauncher://connect/&lt;address&gt;</c> web-link form. Extracts the address
/// only; the connect pipeline validates it, exactly as for <c>--connect</c>.
/// </summary>
public static class LauncherUri
{
    public const string Scheme = "cfxlauncher";

    private const string ConnectPrefix = Scheme + "://connect/";

    public static string? TryGetConnectAddress(string argument)
    {
        if (!argument.StartsWith(ConnectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rest = argument[ConnectPrefix.Length..];

        // Queries and fragments are ignored: sites append tracking parameters, and no
        // connectable address carries a raw '?' or '#'.
        var cut = rest.IndexOfAny(['?', '#']);

        if (cut >= 0)
        {
            rest = rest[..cut];
        }

        var address = Uri.UnescapeDataString(rest).TrimEnd('/');

        // Transport hygiene, not validation: a control character (a decoded newline above
        // all) would break the one-line pipe protocol and truncate the address.
        return address.Length == 0 || address.Any(char.IsControl) ? null : address;
    }
}
