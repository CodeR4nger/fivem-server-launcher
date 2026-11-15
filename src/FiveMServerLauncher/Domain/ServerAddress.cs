namespace FiveMServerLauncher.Domain;

public enum ServerAddressKind
{
    Unknown,
    CfxId,
    CfxJoinUrl,
    IpPort,
    DomainPort,
    IpAddress,
    DomainName
}

public static class ServerAddress
{
    public const string Prefix = "cfx.re/join/";
    public const int DefaultPort = 30120;

    public static bool IsDirectAddress(ServerAddressKind kind)
    {
        return kind is ServerAddressKind.IpPort or ServerAddressKind.DomainPort
            or ServerAddressKind.IpAddress or ServerAddressKind.DomainName;
    }

    public static bool IsIdForm(ServerAddressKind kind)
    {
        return kind is ServerAddressKind.CfxId or ServerAddressKind.CfxJoinUrl;
    }

    public static bool IsLoopbackAddress(string address)
    {
        var kind = Classify(address);

        return kind switch
        {
            ServerAddressKind.IpPort or ServerAddressKind.DomainPort
                when TrySplitHostPort(address, out var host, out _) && IsLoopbackHost(host) => true,
            ServerAddressKind.IpAddress when IsLoopbackHost(address) => true,
            ServerAddressKind.DomainName when IsLocalhost(address) => true,
            _ => false
        };
    }

    private static bool IsLoopbackHost(string host)
    {
        return host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) || IsLocalhost(host);
    }

    public static ServerAddressKind Classify(string address)
    {
        if (IsCfxJoinUrlWithValidId(address))
        {
            return ServerAddressKind.CfxJoinUrl;
        }

        if (!string.IsNullOrWhiteSpace(address) && !address.Any(char.IsWhiteSpace))
        {
            if (TrySplitHostPort(address, out var host, out var port) && IsValidPort(port))
            {
                if (IsIpv4(host))
                {
                    return ServerAddressKind.IpPort;
                }

                if (IsValidDomain(host))
                {
                    return ServerAddressKind.DomainPort;
                }
            }

            if (IsIpv4(address))
            {
                return ServerAddressKind.IpAddress;
            }

            if (IsLocalhost(address) || (address.Contains('.') && IsValidDomain(address)))
            {
                return ServerAddressKind.DomainName;
            }

            if (IsValidCfxId(address) && !address.Contains('.') && !address.Contains(':'))
            {
                return ServerAddressKind.CfxId;
            }
        }

        return ServerAddressKind.Unknown;
    }

    private static bool IsCfxJoinUrlWithValidId(string address)
    {
        return TryGetJoinId(address, out var id) && IsValidCfxId(id);
    }

    /// <summary>
    /// The anchored join form: <c>[http://|https://][www.]cfx.re/join/&lt;rest&gt;</c>,
    /// case-insensitive. Nothing else may precede the prefix, so arbitrary text (for
    /// example shortcut argument injection) can never ride a join-form classification.
    /// </summary>
    private static bool TryGetJoinId(string address, out string id)
    {
        var rest = TrimStart(address, "https://", "http://");
        rest = TrimStart(rest, "www.");

        var matched = rest.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        id = matched ? rest[Prefix.Length..] : string.Empty;
        return matched;
    }

    private static string TrimStart(string text, params string[] alternatives)
    {
        foreach (var prefix in alternatives)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return text[prefix.Length..];
            }
        }

        return text;
    }

    public static bool IsValidCfxId(string id)
    {
        return id.Length > 0 && id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
    }

    private static bool IsLocalhost(string address)
    {
        return address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidDomain(string host)
    {
        var labels = host.Split('.');
        if (labels.Any(string.IsNullOrEmpty))
        {
            return false;
        }

        var allLabelsValid = labels.All(IsValidLabel);
        var tldHasLetter = labels.Length < 2 || labels[^1].Any(char.IsLetter);

        return allLabelsValid && tldHasLetter;
    }

    private static bool IsValidLabel(string label)
    {
        return label.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    private static bool IsValidPort(string portText)
    {
        return int.TryParse(portText, out var port) && port is >= 1 and <= 65535;
    }

    private static bool IsIpv4(string ip)
    {
        var octets = ip.Split('.');
        if (octets.Length != 4)
        {
            return false;
        }

        foreach (var octet in octets)
        {
            if (!byte.TryParse(octet, out var value) || !octet.Equals(value.ToString()))
            {
                return false;
            }
        }

        return true;
    }

    public static string FromCfxId(string cfxId)
    {
        return $"{Prefix}{cfxId}";
    }

    public static bool TrySplitHostPort(string address, out string host, out string port)
    {
        var separator = address.LastIndexOf(':');
        if (separator <= 0 || separator == address.Length - 1)
        {
            host = string.Empty;
            port = string.Empty;
            return false;
        }

        host = address[..separator];
        port = address[(separator + 1)..];
        return true;
    }

    public static string ExtractCfxId(string address)
    {
        return TryGetJoinId(address, out var id) ? id : address;
    }

    public static bool HasServerFormWithNonEmptyId(string address)
    {
        if (!address.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsValidCfxId(address[Prefix.Length..]);
    }
}