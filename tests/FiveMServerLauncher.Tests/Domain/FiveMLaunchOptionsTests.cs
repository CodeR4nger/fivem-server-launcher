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
    public void Create_WithUnknownGameClient_ShouldThrow()
    {
        // Given
        const GameClient unknownClient = (GameClient)99;

        // When / Then
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FiveMLaunchOptions.Create(null, unknownClient, null, null, false));
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
    public void ToCommandLineArgs_ShouldReturnReadOnlyList()
    {
        // Given
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3258, 1, false);

        // When
        var args = options.ToCommandLineArgs();
        var mutable = args as IList<string>;

        // Then
        Assert.NotNull(mutable);
        Assert.True(mutable.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutable.Add("-b1234"));
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
        var options = FiveMLaunchOptions.Create(null, GameClient.FiveM, 3258, 1, true);

        // When
        var modified = options with { GameBuild = 0 };

        // Then
        Assert.Equal(3258, options.GameBuild);
        Assert.Equal(1, options.PureMode);
        Assert.False(ReferenceEquals(options, modified));
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