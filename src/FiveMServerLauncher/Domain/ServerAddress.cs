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

            if (address.Contains('.') && IsValidDomain(address))
            {
                return ServerAddressKind.DomainName;
            }

            if (!address.Contains('.') && !address.Contains(':'))
            {
                return ServerAddressKind.CfxId;
            }
        }

        return ServerAddressKind.Unknown;
    }

    private static bool IsCfxJoinUrlWithValidId(string address)
    {
        var index = address.IndexOf(Prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        return IsValidCfxId(address[(index + Prefix.Length)..]);
    }

    private static bool IsValidCfxId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && !id.Any(char.IsWhiteSpace);
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
        var index = address.IndexOf(Prefix, StringComparison.OrdinalIgnoreCase);

        return index >= 0
            ? address[(index + Prefix.Length)..]
            : address;
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