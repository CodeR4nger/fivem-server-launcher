namespace FiveMServerLauncher.Domain;

public static class ServerAddress
{
    public const string Prefix = "cfx.re/join/";

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

        var id = address[Prefix.Length..];

        return !string.IsNullOrWhiteSpace(id) && !id.Any(char.IsWhiteSpace);
    }
}