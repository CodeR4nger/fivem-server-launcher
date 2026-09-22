using System.Text.Json.Serialization;

namespace FiveMServerLauncher.Domain;

public sealed class SavedServer
{
    [JsonConstructor]
    private SavedServer(string name, string address, bool? requiresSteam, bool? requiresDiscord, string? cfxId)
    {
        Name = name;
        Address = address;
        RequiresSteam = requiresSteam;
        RequiresDiscord = requiresDiscord;
        CfxId = cfxId;
    }

    public string Name { get; }

    public string Address { get; }

    public bool? RequiresSteam { get; }

    public bool? RequiresDiscord { get; }

    public string? CfxId { get; }

    public bool MatchesAddress(string address)
    {
        return Address.Equals(address, StringComparison.OrdinalIgnoreCase);
    }

    public static SavedServer Create(
        string name,
        string address,
        bool? requiresSteam = null,
        bool? requiresDiscord = null,
        string? cfxId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var normalizedAddress = address.Trim();
        var kind = ServerAddress.Classify(normalizedAddress);
        if (kind == ServerAddressKind.Unknown)
        {
            throw new ArgumentException($"'{address}' is not a connectable server address.", nameof(address));
        }

        var resolvedCfxId = cfxId ?? (kind is ServerAddressKind.CfxId or ServerAddressKind.CfxJoinUrl
            ? ServerAddress.ExtractCfxId(normalizedAddress)
            : null);

        return new SavedServer(name.Trim(), normalizedAddress, requiresSteam, requiresDiscord, resolvedCfxId);
    }
}