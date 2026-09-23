using System.Collections.Immutable;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain.Exceptions;
using GameClientEnum = FiveMServerLauncher.Core.Enums.GameClient;

namespace FiveMServerLauncher.Domain;

public sealed class FiveMLaunchOptions
{
    public string? Address { get; }
    public GameClient? GameClient { get; }
    public int? GameBuild { get; }
    public int? PureMode { get; }
    public bool SecondClient { get; }

    private FiveMLaunchOptions(string? address, GameClient? gameClient, int? gameBuild, int? pureMode, bool secondClient)
    {
        Address = address;
        GameClient = gameClient;
        GameBuild = gameBuild;
        PureMode = pureMode;
        SecondClient = secondClient;
    }

    public static FiveMLaunchOptions FromServerProfile(ServerProfile profile)
    {
        return Create(
            ProfileAddress(profile),
            profile.GameClient,
            profile.Requirements.GameBuild,
            profile.Requirements.PureMode,
            false);
    }

    private static string? ProfileAddress(ServerProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.CfxId))
        {
            return ServerAddress.FromCfxId(profile.CfxId);
        }

        return profile.Address;
    }

    public Uri? ToUri()
    {
        if (Address is null || IsEnhanced())
        {
            return null;
        }

        var scheme = GameClient == GameClientEnum.RedM ? "redm" : "fivem";
        var uri = $"{scheme}://connect/{Address}";

        foreach (var arg in BuildGameFlags())
        {
            uri += $"?{arg}";
        }

        return new Uri(uri);
    }

    public ImmutableArray<string> ToCommandLineArgs()
    {
        if (IsEnhanced())
        {
            return ImmutableArray<string>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        builder.AddRange(BuildGameFlags());

        if (SecondClient)
        {
            builder.Add("-cl2");
        }

        return builder.ToImmutable();
    }

    private bool IsEnhanced()
    {
        return GameClient == GameClientEnum.FiveMEnhanced;
    }

    private IEnumerable<string> BuildGameFlags()
    {
        if (GameBuild.HasValue)
        {
            yield return $"-b{GameBuild.Value}";
        }

        if (PureMode.HasValue)
        {
            yield return $"-pure_{PureMode.Value}";
        }
    }

    public static FiveMLaunchOptions Create(
        string? address,
        GameClient? gameClient,
        int? gameBuild,
        int? pureMode,
        bool secondClient)
    {
        ValidateAddress(address);
        ValidateGameClient(gameClient);

        return new FiveMLaunchOptions(address, gameClient, gameBuild, pureMode, secondClient);
    }

    private static void ValidateAddress(string? address)
    {
        if (address is null)
        {
            return;
        }

        var kind = ServerAddress.Classify(address);
        if (kind is ServerAddressKind.CfxJoinUrl or ServerAddressKind.IpPort
            or ServerAddressKind.DomainPort or ServerAddressKind.IpAddress
            or ServerAddressKind.DomainName)
        {
            return;
        }

        throw new InvalidAddressException(address);
    }

    private static void ValidateGameClient(GameClient? gameClient)
    {
        if (gameClient is not null && !Enum.IsDefined(gameClient.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(gameClient), gameClient, "Unknown game client.");
        }
    }
}
