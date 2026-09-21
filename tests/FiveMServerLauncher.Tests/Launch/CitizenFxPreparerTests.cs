using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Domain;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

public class CitizenFxPreparerTests
{
    private static ServerProfile LegacyProfile(ServerRequirements requirements) => new()
    {
        IsCfxValidated = true,
        GameClient = GameClient.FiveM,
        CfxId = "y4lg95",
        Requirements = requirements,
    };

    [Fact]
    public async Task Prime_WhenDefaultBuildPublished_ShouldWriteDefaultBuildKey()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements { DefaultBuild = 3788 }));

        // Then
        var (iniPath, values) = Assert.Single(writer.Calls);
        Assert.EndsWith("CitizenFX.ini", iniPath);
        Assert.Equal("3788", values["DefaultBuild"]);
        Assert.Equal(string.Empty, values["PoolSizesIncrease"]);
        Assert.Equal(2, values.Count);
    }

    [Fact]
    public async Task Prime_WhenDefaultBuildAbsentButReplaceExecutable_ShouldWriteEnforcedGameBuild()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements
        {
            ReplaceExecutable = true,
            GameBuild = 3258,
        }));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Equal("3258", values["DefaultBuild"]);
    }

    [Fact]
    public async Task Prime_WhenDefaultBuildAndReplaceExecutableBothPublished_ShouldPreferDefaultBuild()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements
        {
            DefaultBuild = 3788,
            ReplaceExecutable = true,
            GameBuild = 3258,
        }));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Equal("3788", values["DefaultBuild"]);
    }

    [Fact]
    public async Task Prime_WhenDefaultBuildIsZeroAndReplaceExecutable_ShouldFallBackToEnforcedBuild()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements
        {
            DefaultBuild = 0,
            ReplaceExecutable = true,
            GameBuild = 3258,
        }));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Equal("3258", values["DefaultBuild"]);
    }

    [Fact]
    public async Task Prime_WhenDefaultBuildIsZeroAndNoFallback_ShouldOnlyResetPoolSizes()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements { DefaultBuild = 0 }));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Single(values);
        Assert.Equal(string.Empty, values["PoolSizesIncrease"]);
    }

    [Fact]
    public async Task Prime_WhenPoolSizesIsWhitespace_ShouldResetToEmpty()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements { PoolSizesIncrease = "   " }));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Equal(string.Empty, values["PoolSizesIncrease"]);
    }

    [Fact]
    public async Task Prime_WhenPoolSizesIncreasePublished_ShouldWriteItVerbatim()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements
        {
            PoolSizesIncrease = "{\"CWeaponComponentInfo\":500}",
        }));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Equal("{\"CWeaponComponentInfo\":500}", values["PoolSizesIncrease"]);
    }

    [Fact]
    public async Task Prime_WhenAllFactsPublished_ShouldWriteDefaultBuildAndPoolSizes()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements
        {
            DefaultBuild = 3788,
            PoolSizesIncrease = "{\"CWeaponComponentInfo\":500}",
        }));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Equal("3788", values["DefaultBuild"]);
        Assert.Equal("{\"CWeaponComponentInfo\":500}", values["PoolSizesIncrease"]);
    }

    [Fact]
    public async Task Prime_WhenNoRelevantFactsPublished_ShouldResetPoolSizesToEmpty()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements()));

        // Then
        var (_, values) = Assert.Single(writer.Calls);
        Assert.Single(values);
        Assert.Equal(string.Empty, values["PoolSizesIncrease"]);
    }

    [Fact]
    public async Task Prime_WhenProfileNotCfxValidated_ShouldNotWrite()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        var profile = LegacyProfile(new ServerRequirements { DefaultBuild = 3788 });
        profile.IsCfxValidated = false;

        // When
        await preparer.PrimeAsync(profile);

        // Then
        Assert.Empty(writer.Calls);
    }

    [Fact]
    public async Task Prime_WhenGameClientIsEnhanced_ShouldNotWrite()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        var profile = LegacyProfile(new ServerRequirements { DefaultBuild = 3788 });
        profile.GameClient = GameClient.FiveMEnhanced;

        // When
        await preparer.PrimeAsync(profile);

        // Then
        Assert.Empty(writer.Calls);
    }

    [Fact]
    public async Task Prime_WhenGameClientIsRedM_ShouldNotWrite()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        var profile = LegacyProfile(new ServerRequirements { DefaultBuild = 3788 });
        profile.GameClient = GameClient.RedM;

        // When
        await preparer.PrimeAsync(profile);

        // Then
        Assert.Empty(writer.Calls);
    }

    [Fact]
    public async Task Prime_WhenGameClientUnknown_ShouldNotWrite()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        var profile = LegacyProfile(new ServerRequirements { DefaultBuild = 3788 });
        profile.GameClient = null;

        // When
        await preparer.PrimeAsync(profile);

        // Then
        Assert.Empty(writer.Calls);
    }

    [Fact]
    public async Task Prime_WhenLegacyNotInstalled_ShouldNotWrite()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(
            new FakeClientInstallLocator { Executables = [] },
            writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements { DefaultBuild = 3788 }));

        // Then
        Assert.Empty(writer.Calls);
    }

    [Fact]
    public async Task Prime_WhenWriterThrows_ShouldNotThrow()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter { Throw = true };
        var preparer = new CitizenFxPreparer(new FakeClientInstallLocator(), writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements { DefaultBuild = 3788 }));

        // Then
        Assert.Empty(writer.Calls);
    }

    [Fact]
    public async Task Prime_WhenLocatorThrows_ShouldNotThrow()
    {
        // Given
        var writer = new FakeCitizenFxConfigWriter();
        var preparer = new CitizenFxPreparer(
            new FakeClientInstallLocator { Throw = true },
            writer);

        // When
        await preparer.PrimeAsync(LegacyProfile(new ServerRequirements { DefaultBuild = 3788 }));

        // Then
        Assert.Empty(writer.Calls);
    }
}