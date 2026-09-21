using System.Text.Json.Serialization;

namespace FiveMServerLauncher.Domain;

public sealed class SavedServer
{
    [JsonConstructor]
    private SavedServer(string name, string address, bool? requiresSteam, bool? requiresDiscord)
    {
        Name = name;
        Address = address;
        RequiresSteam = requiresSteam;
        RequiresDiscord = requiresDiscord;
    }

    public string Name { get; }

    public string Address { get; }

    public bool? RequiresSteam { get; }

    public bool? RequiresDiscord { get; }

    public bool MatchesAddress(string address)
    {
        return Address.Equals(address, StringComparison.OrdinalIgnoreCase);
    }

    public static SavedServer Create(string name, string address, bool? requiresSteam = null, bool? requiresDiscord = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var normalizedAddress = address.Trim();
        if (ServerAddress.Classify(normalizedAddress) == ServerAddressKind.Unknown)
        {
            throw new ArgumentException($"'{address}' is not a connectable server address.", nameof(address));
        }

        return new SavedServer(name.Trim(), normalizedAddress, requiresSteam, requiresDiscord);
    }
}