using System.Text.Json.Serialization;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Domain;

public sealed class SavedServer
{
    [JsonConstructor]
    private SavedServer(
        string name,
        string address,
        bool? requiresSteam,
        bool? requiresDiscord,
        string? cfxId,
        int? gameBuild,
        int? pureMode,
        GameClient? gameClient)
    {
        Name = name;
        Address = address;
        RequiresSteam = requiresSteam;
        RequiresDiscord = requiresDiscord;
        CfxId = cfxId;
        GameBuild = gameBuild;
        PureMode = pureMode;
        GameClient = gameClient;
    }

    public string Name { get; }

    public string Address { get; }

    public bool? RequiresSteam { get; }

    public bool? RequiresDiscord { get; }

    public string? CfxId { get; }

    public int? GameBuild { get; }

    public int? PureMode { get; }

    public GameClient? GameClient { get; }

    public bool MatchesAddress(string address)
    {
        return Address.Equals(address, StringComparison.OrdinalIgnoreCase);
    }

    public bool MatchesCfxId(string cfxId)
    {
        return CfxId is not null && CfxId.Equals(cfxId, StringComparison.OrdinalIgnoreCase);
    }

    public static SavedServer Create(
        string name,
        string address,
        bool? requiresSteam = null,
        bool? requiresDiscord = null,
        string? cfxId = null,
        int? gameBuild = null,
        int? pureMode = null,
        GameClient? gameClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var normalizedAddress = address.Trim();
        var kind = ServerAddress.Classify(normalizedAddress);
        if (kind == ServerAddressKind.Unknown)
        {
            throw new ArgumentException($"'{address}' is not a connectable server address.", nameof(address));
        }

        if (cfxId is not null && !ServerAddress.IsValidCfxId(cfxId))
        {
            throw new ArgumentException($"'{cfxId}' is not a valid cfx id.", nameof(cfxId));
        }

        var resolvedCfxId = cfxId ?? (ServerAddress.IsIdForm(kind)
            ? ServerAddress.ExtractCfxId(normalizedAddress)
            : null);

        return new SavedServer(
            name.Trim(), normalizedAddress, requiresSteam, requiresDiscord,
            resolvedCfxId, gameBuild, pureMode, gameClient);
    }
}