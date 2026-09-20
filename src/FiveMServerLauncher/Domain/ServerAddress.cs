namespace FiveMServerLauncher.Domain;

public enum ServerAddressKind
{
    Unknown,
    CfxId,
    CfxJoinUrl,
    IpPort,
    DomainPort
}

public static class ServerAddress
{
    public const string Prefix = "cfx.re/join/";

    public static ServerAddressKind Classify(string address)
    {
        if (IsCfxJoinUrlWithValidId(address))
        {
            return ServerAddressKind.CfxJoinUrl;
        }

        if (!string.IsNullOrWhiteSpace(address) && !address.Any(char.IsWhiteSpace))
        {
            if (IsIpPort(address))
            {
                return ServerAddressKind.IpPort;
            }

            if (IsDomainPort(address))
            {
                return ServerAddressKind.DomainPort;
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

    private static bool IsCfxJoinUrl(string address)
    {
        return address.IndexOf(Prefix, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsIpPort(string address)
    {
        var separator = address.LastIndexOf(':');
        if (separator <= 0 || separator == address.Length - 1)
        {
            return false;
        }

        var ip = address[..separator];
        var portText = address[(separator + 1)..];

        return IsValidPort(portText) && IsIpv4(ip);
    }

    private static bool IsDomainPort(string address)
    {
        var separator = address.LastIndexOf(':');
        if (separator <= 0 || separator == address.Length - 1)
        {
            return false;
        }

        var host = address[..separator];
        var portText = address[(separator + 1)..];

        return IsValidPort(portText) && IsValidDomain(host);
    }

    private static bool IsValidDomain(string host)
    {
        var labels = host.Split('.');
        if (labels.Length < 2 || labels.Any(string.IsNullOrEmpty))
        {
            return false;
        }

        var allLabelsValid = labels.All(IsValidLabel);
        var tldHasLetter = labels[^1].Any(char.IsLetter);

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