using System.Collections.Immutable;
using System.Reflection;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Domain.Exceptions;

namespace FiveMServerLauncher.Tests.Domain;

public class FiveMLaunchOptionsTests
{
    [Fact]
    public void Create_WithKnownFields_ShouldExposeThem()
    {
        // Given
        var address = "cfx.re/join/y4lg95";
        const GameClient gameClient = GameClient.FiveM;
        const int gameBuild = 3258;
        const int pureMode = 1;
        const bool secondClient = true;

        // When
        var options = FiveMLaunchOptions.Create(
            address,
            gameClient,
            gameBuild,
            pureMode,
            secondClient);

        // Then
        Assert.Equal(address, options.Address);
        Assert.Equal(gameClient, options.GameClient);
        Assert.Equal(gameBuild, options.GameBuild);
        Assert.Equal(pureMode, options.PureMode);
        Assert.True(options.SecondClient);
    }

    [Fact]
    public void Create_WithoutAddress_ShouldAllowDirectOpen()
    {
        // Given
        const GameClient gameClient = GameClient.FiveM;

        // When
        var options = FiveMLaunchOptions.Create(null, gameClient, null, null, false);

        // Then
        Assert.Null(options.Address);
    }

    [Fact]
    public void Create_WithMalformedAddress_ShouldThrow()
    {
        // Given
        const string malformedAddress = "";
        const GameClient gameClient = GameClient.FiveM;

        // When / Then
        Assert.Throws<InvalidAddressException>(() =>
            FiveMLaunchOptions.Create(malformedAddress, gameClient, null, null, false));
    }

    [Fact]
    public void Create_WithAddressNotInServerForm_ShouldThrow()
    {
        // Given
        const string notServerForm = "y4lg95";
        const GameClient gameClient = GameClient.FiveM;

        // When / Then
        Assert.Throws<InvalidAddressException>(() =>
            FiveMLaunchOptions.Create(notServerForm, gameClient, null, null, false));
    }

    [Fact]
    public void Create_WithIpPortAddress_ShouldAccept()
    {
        // Given
        const string ipPort = "149.56.120.52:30320";
        const GameClient gameClient = GameClient.FiveM;

        // When
        var options = FiveMLaunchOptions.Create(ipPort, gameClient, null, null, false);

        // Then
        Assert.Equal(ipPort, options.Address);
    }

    [Fact]
    public void Create_WithDomainPortAddress_ShouldAccept()
    {
        // Given
        const string domainPort = "play.example.com:30120";
        const GameClient gameClient = GameClient.FiveM;

        // When
        var options = FiveMLaunchOptions.Create(domainPort, gameClient, null, null, false);

        // Then
        Assert.Equal(domainPort, options.Address);
    }

    [Fact]
    public void Create_WithMalformedIpPort_ShouldThrow()
    {
        // Given
        const string malformedIpPort = "999.56.120.52:30320";
        const GameClient gameClient = GameClient.FiveM;

        // When / Then
        Assert.Throws<InvalidAddressException>(() =>
            FiveMLaunchOptions.Create(malformedIpPort, gameClient, null, null, false));
    }

    [Fact]
    public void Create_WithBareIpAddress_ShouldAcceptAndEmitAsIs()
    {
        // Given
        const string bareIp = "149.56.120.52";
        const GameClient gameClient = GameClient.FiveM;

        // When
        var options = FiveMLaunchOptions.Create(bareIp, gameClient, null, null, false);

        // Then
        Assert.Equal(bareIp, options.Address);
        Assert.Equal(new Uri($"fivem://connect/{bareIp}"), options.ToUri());
    }

    [Fact]
    public void Create_WithBareDomainName_ShouldAcceptAndEmitAsIs()
    {
        // Given
        const string bareDomain = "play.example.com";
        const GameClient gameClient = GameClient.FiveM;

        // When
        var options = FiveMLaunchOptions.Create(bareDomain, gameClient, null, null, false);

        // Then
        Assert.Equal(bareDomain, options.Address);
        Assert.Equal(new Uri($"fivem://connect/{bareDomain}"), options.ToUri());
    }

    [Fact]
    public void Create_WithAddressMissingId_ShouldThrow()
    {
        // Given
        const string missingId = "cfx.re/join/";
        const GameClient gameClient = GameClient.FiveM;

        // When / Then
        Assert.Throws<InvalidAddressException>(() =>
            FiveMLaunchOptions.Create(missingId, gameClient, null, null, false));
    }

    [Fact]
    public void Create_WithAddressTrailingWhitespace_ShouldThrow()
    {
        // Given
        const string trailingWhitespace = "cfx.re/join/y4lg95 ";
        const GameClient gameClient = GameClient.FiveM;

        // When / Then
        Assert.Throws<InvalidAddressException>(() =>
            FiveMLaunchOptions.Create(trailingWhitespace, gameClient, null, null, false));
    }

    [Fact]
    public void Create_WithAddressContainingInternalWhitespace_ShouldThrow()
    {
        // Given
        const string internalWhitespace = "cfx.re/join/y4 lg95";
        const GameClient gameClient = GameClient.FiveM;

        // When / Then
        Assert.Throws<InvalidAddressException>(() =>
            FiveMLaunchOptions.Create(internalWhitespace, gameClient, null, null, false));
    }

    [Fact]
    public void Create_ShouldBeOnlyPublicWayToConstruct()
    {
        // Given
        var parameterizedCtor = typeof(FiveMLaunchOptions).GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            new[] { typeof(string), typeof(GameClient?), typeof(int?), typeof(int?), typeof(bool) },
            modifiers: null);

        // When / Then
        Assert.Null(parameterizedCtor);
    }

    [Fact]
    public void Type_ShouldHaveNoMutatingAccessors()
    {
        // Given / When
        var cloneMethod = typeof(FiveMLaunchOptions).GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance);

        // Then
        Assert.Empty(GetSetters(typeof(FiveMLaunchOptions)));
        Assert.Null(cloneMethod);
    }

    [Fact]
    public void Create_WithUnknownGameClient_ShouldThrow()
    {
        // Given
        const GameClient unknownClient = (GameClient)99;

        // When / Then
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FiveMLaunchOptions.Create(null, unknownClient, null, null, false));
    }

    [Fact]
    public void ToUri_WithRedMProfile_ShouldUseRedmScheme()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.RedM,
            Requirements = new ServerRequirements { GameBuild = 3258, PureMode = 1 },
        };

        // When
        var uri = FiveMLaunchOptions.FromServerProfile(profile).ToUri();

        // Then
        Assert.Equal("redm://connect/cfx.re/join/y4lg95?-b3258?-pure_1", uri?.AbsoluteUri);
    }

    [Fact]
    public void ToUri_GivenServerProfile_ShouldReturnConnectUri()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements(),
        };

        // When
        var uri = FiveMLaunchOptions.FromServerProfile(profile).ToUri();

        // Then
        Assert.Equal("fivem://connect/cfx.re/join/y4lg95", uri?.AbsoluteUri);
    }

    [Fact]
    public void ToUri_WithGameBuildAndPureMode_ShouldAppendParams()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements
            {
                GameBuild = 3258,
                PureMode = 1,
            },
        };

        // When
        var uri = FiveMLaunchOptions.FromServerProfile(profile).ToUri();

        // Then
        Assert.Equal(
            "fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1",
            uri?.AbsoluteUri);
    }

    [Fact]
    public void ToUri_WithOnlyGameBuild_ShouldAppendGameBuildParam()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements { GameBuild = 1604 },
        };

        // When
        var uri = FiveMLaunchOptions.FromServerProfile(profile).ToUri();

        // Then
        Assert.Equal(
            "fivem://connect/cfx.re/join/y4lg95?-b1604",
            uri?.AbsoluteUri);
    }

    [Fact]
    public void ToUri_WithOnlyPureMode_ShouldAppendPureModeParam()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveM,
            Requirements = new ServerRequirements { PureMode = 2 },
        };

        // When
        var uri = FiveMLaunchOptions.FromServerProfile(profile).ToUri();

        // Then
        Assert.Equal(
            "fivem://connect/cfx.re/join/y4lg95?-pure_2",
            uri?.AbsoluteUri);
    }

    [Fact]
    public void ToUri_WithFiveMEnhanced_ShouldReturnNull()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.FiveMEnhanced,
            Requirements = new ServerRequirements(),
        };

        // When
        var uri = FiveMLaunchOptions.FromServerProfile(profile).ToUri();

        // Then
        Assert.Null(uri);
    }

    [Fact]
    public void ToUri_WithoutAddress_ShouldReturnNull()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, null, null, false);

        // When
        var uri = options.ToUri();

        // Then
        Assert.Null(uri);
    }

    [Fact]
    public void ToCommandLineArgs_WithBuildAndPureAndSecond_ShouldReturnAllArgs()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3258, 1, true);

        // When
        var args = options.ToCommandLineArgs();

        // Then
        Assert.IsNotType<string[]>(args);
        Assert.IsType<ImmutableArray<string>>(args);
        Assert.Equal(new[] { "-b3258", "-pure_1", "-cl2" }, args);
    }

    [Fact]
    public void ToCommandLineArgs_WithNoRequirementsOrFlags_ShouldReturnEmpty()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, null, null, false);

        // When
        var args = options.ToCommandLineArgs();

        // Then
        Assert.Empty(args);
    }

    [Fact]
    public void ToCommandLineArgs_WithFiveMEnhanced_ShouldReturnEmpty()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveMEnhanced, 3258, 1, true);

        // When
        var args = options.ToCommandLineArgs();

        // Then
        Assert.Empty(args);
    }

    [Fact]
    public void ToCommandLineArgs_ShouldReturnImmutableArray()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3258, 1, false);

        // When
        var args = options.ToCommandLineArgs();

        // Then
        Assert.IsNotType<string[]>(args);
        Assert.IsType<ImmutableArray<string>>(args);
        Assert.Equal(new[] { "-b3258", "-pure_1" }, args);
    }

    [Fact]
    public void ToCommandLineArgs_ShouldRejectMutationByIndexerCast()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3258, 1, false);

        // When
        var args = options.ToCommandLineArgs();
        var mutable = args as IList<string>;

        // Then
        Assert.NotNull(mutable);
        Assert.Throws<NotSupportedException>(() => mutable[0] = "-b1234");
        Assert.Equal(new[] { "-b3258", "-pure_1" }, args);
    }

    [Fact]
    public void FromServerProfile_ShouldTakeRequirementsFromProfile()
    {
        // Given
        var profile = new ServerProfile
        {
            CfxId = "y4lg95",
            ProjectName = "Test Server",
            GameClient = GameClient.RedM,
            Requirements = new ServerRequirements { GameBuild = 2944, PureMode = 0 },
        };

        // When
        var options = FiveMLaunchOptions.FromServerProfile(profile);

        // Then
        Assert.Equal(2944, options.GameBuild);
        Assert.Equal(0, options.PureMode);
        Assert.Equal(GameClient.RedM, options.GameClient);
    }

    [Fact]
    public void Value_ShouldBeImmutableAfterConstruction()
    {
        // Given
        var first = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3258, 2, true);
        var second = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3258, 2, true);

        // When / Then
        Assert.Empty(GetSetters(typeof(FiveMLaunchOptions)));
        Assert.Equal(first.Address, second.Address);
        Assert.Equal(first.GameClient, second.GameClient);
        Assert.Equal(first.GameBuild, second.GameBuild);
        Assert.Equal(first.PureMode, second.PureMode);
        Assert.Equal(first.SecondClient, second.SecondClient);
    }

    private static MethodInfo[] GetSetters(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.SetMethod)
            .Where(setter => setter is not null)
            .ToArray();
    }

    [Fact]
    public void ToCommandLineArgs_WithAddressAndFlags_ShouldNotIncludeServerAddress()
    {
        // Given
        var options = FiveMLaunchOptions.Create("cfx.re/join/y4lg95", GameClient.FiveM, 3258, 1, true);

        // When
        var args = options.ToCommandLineArgs();

        // Then
        Assert.DoesNotContain("cfx.re/join/y4lg95", args);
        Assert.Equal(new[] { "-b3258", "-pure_1", "-cl2" }, args);
    }
}