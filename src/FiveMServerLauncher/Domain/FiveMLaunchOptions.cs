using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain.Exceptions;
using GameClientEnum = FiveMServerLauncher.Core.Enums.GameClient;

namespace FiveMServerLauncher.Domain;

public sealed record FiveMLaunchOptions
{
    public string? Address { get; init; }
    public GameClient? GameClient { get; init; }
    public int? GameBuild { get; init; }
    public int? PureMode { get; init; }
    public bool SecondClient { get; init; }

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
            ServerAddress.FromCfxId(profile.CfxId),
            profile.GameClient,
            profile.Requirements.GameBuild,
            profile.Requirements.PureMode,
            false);
    }

    public Uri? ToUri()
    {
        if (Address is null || IsEnhanced())
        {
            return null;
        }

        var uri = $"fivem://connect/{Address}";

        foreach (var arg in BuildGameFlags())
        {
            uri += $"?{arg}";
        }

        return new Uri(uri);
    }

    public IReadOnlyList<string> ToCommandLineArgs()
    {
        if (IsEnhanced())
        {
            return Array.Empty<string>();
        }

        var args = new List<string>(BuildGameFlags());

        if (SecondClient)
        {
            args.Add("-cl2");
        }

        return args.ToArray();
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
        if (address is null || ServerAddress.HasServerFormWithNonEmptyId(address))
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
