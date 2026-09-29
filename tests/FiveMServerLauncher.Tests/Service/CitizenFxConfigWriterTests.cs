using FiveMServerLauncher.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.Service;

public class CitizenFxConfigWriterTests
{
    [Fact]
    public async Task Apply_WhenFileDoesNotExist_ShouldCreateFileWithGameKeys()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        var writer = new CitizenFxConfigWriter();
        var values = new Dictionary<string, string>
        {
            ["DefaultBuild"] = "3788",
            ["PoolSizesIncrease"] = "{\"CWeaponComponentInfo\":500}",
        };

        // When
        await writer.ApplyAsync(temp.IniPath, values);

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("[Game]", content);
        Assert.Contains("DefaultBuild=3788", content);
        Assert.Contains("PoolSizesIncrease={\"CWeaponComponentInfo\":500}", content);
    }

    [Fact]
    public async Task Apply_ShouldReplaceExistingGameKeyValue()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        File.WriteAllText(temp.IniPath, "[Game]\r\nDefaultBuild=3258\r\n");

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["DefaultBuild"] = "3788",
        });

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("DefaultBuild=3788", content);
        Assert.DoesNotContain("DefaultBuild=3258", content);
    }

    [Fact]
    public async Task Apply_WhenValueUnchanged_ShouldNotRewriteFile()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        const string original = "[Game]\r\nDefaultBuild=3788\r\n";
        File.WriteAllText(temp.IniPath, original);
        File.SetAttributes(temp.IniPath, FileAttributes.ReadOnly);

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["DefaultBuild"] = "3788",
        });

        // Then
        Assert.Equal(original, File.ReadAllText(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenSectionMissing_ShouldAppendGameSection()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        File.WriteAllText(temp.IniPath, "[Other]\r\nSomeKey=42\r\n");

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["DefaultBuild"] = "3788",
        });

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("[Game]", content);
        Assert.Contains("DefaultBuild=3788", content);
        Assert.Contains("[Other]", content);
        Assert.Contains("SomeKey=42", content);
    }

    [Fact]
    public async Task Apply_WhenKeyMissingInGameSection_ShouldAppendKey()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        File.WriteAllText(temp.IniPath, "[Game]\r\nDefaultBuild=3788\r\n");

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["PoolSizesIncrease"] = "{\"CWeaponComponentInfo\":500}",
        });

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("DefaultBuild=3788", content);
        Assert.Contains("PoolSizesIncrease={\"CWeaponComponentInfo\":500}", content);
    }

    [Fact]
    public async Task Apply_ShouldPreserveOtherSectionsAndKeysVerbatim()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        const string original = """
                                [Game]
                                IVPath=C:\fivem
                                SavedBuildNumber=2060

                                [Other]
                                SomeKey=42
                                """;
        File.WriteAllText(temp.IniPath, original);

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["DefaultBuild"] = "3788",
        });

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("IVPath=C:\\fivem", content);
        Assert.Contains("SavedBuildNumber=2060", content);
        Assert.Contains("[Other]", content);
        Assert.Contains("SomeKey=42", content);
        Assert.Contains("DefaultBuild=3788", content);
    }

    [Fact]
    public async Task Apply_WhenSameKeyInOtherSection_ShouldOnlyTouchGameSection()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        File.WriteAllText(temp.IniPath, "[Game]\r\nDefaultBuild=3258\r\n[Other]\r\nDefaultBuild=999\r\n");

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["DefaultBuild"] = "3788",
        });

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("DefaultBuild=3788", content);
        Assert.Contains("DefaultBuild=999", content);
    }

    [Fact]
    public async Task Apply_WhenTargetValueIsEmpty_ShouldWriteEmptyValue()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        File.WriteAllText(temp.IniPath, "[Game]\r\nPoolSizesIncrease={\"CWeaponComponentInfo\":500}\r\n");

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["PoolSizesIncrease"] = string.Empty,
        });

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("PoolSizesIncrease=", content);
        Assert.DoesNotContain("{\"CWeaponComponentInfo\":500}", content);
    }

    [Fact]
    public async Task Apply_WhenEmptyValueAlreadySet_ShouldNotRewriteFile()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        const string original = "[Game]\r\nDefaultBuild=3788\r\nPoolSizesIncrease=\r\n";
        File.WriteAllText(temp.IniPath, original);
        File.SetAttributes(temp.IniPath, FileAttributes.ReadOnly);

        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["PoolSizesIncrease"] = string.Empty,
        });

        // Then
        Assert.Equal(original, File.ReadAllText(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenValuesEmpty_ShouldNotTouchFile()
    {
        // Given
        using var temp = new TempCitizenFxIni();
        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>());

        // Then
        Assert.False(File.Exists(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenValueContainsLineBreak_ShouldSkipThatEntry()
    {
        // Given — a server-published value can smuggle line breaks; writing it verbatim
        // would inject arbitrary keys/sections into FiveM's config.
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        const string original = "[Game]\r\nDefaultBuild=3258\r\n";
        File.WriteAllText(temp.IniPath, original);
        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["PoolSizesIncrease"] = "{\"x\":1}\r\n[AntiCheat]\r\nDisabled=1",
        });

        // Then — the poisoned entry is rejected, leaving the file untouched.
        Assert.Equal(original, File.ReadAllText(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenValueContainsLineBreak_ShouldStillWriteSafeEntries()
    {
        // Given — one poisoned value must not block the legitimate ones.
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        File.WriteAllText(temp.IniPath, "[Game]\r\nDefaultBuild=3258\r\n");
        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["DefaultBuild"] = "3788",
            ["PoolSizesIncrease"] = "500\nEvilKey=1",
        });

        // Then
        var content = File.ReadAllText(temp.IniPath);
        Assert.Contains("DefaultBuild=3788", content);
        Assert.DoesNotContain("PoolSizesIncrease", content);
        Assert.DoesNotContain("EvilKey", content);
    }

    [Fact]
    public async Task Apply_WhenKeyContainsLineBreak_ShouldSkipThatEntry()
    {
        // Given — keys are configuration-controlled too; a line break in a key can
        // splice a new section header into the file.
        using var temp = new TempCitizenFxIni();
        Directory.CreateDirectory(temp.DirectoryPath);
        const string original = "[Game]\r\nDefaultBuild=3258\r\n";
        File.WriteAllText(temp.IniPath, original);
        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["DefaultBuild\n[Evil]"] = "1",
        });

        // Then — the poisoned key is rejected, leaving the file untouched.
        Assert.Equal(original, File.ReadAllText(temp.IniPath));
    }

    [Fact]
    public async Task Apply_WhenAllEntriesUnsafeAndFileMissing_ShouldNotCreateFile()
    {
        // Given — degrading the write means never creating a file from unsafe input.
        using var temp = new TempCitizenFxIni();
        var writer = new CitizenFxConfigWriter();

        // When
        await writer.ApplyAsync(temp.IniPath, new Dictionary<string, string>
        {
            ["PoolSizesIncrease"] = "1\r\n2",
        });

        // Then
        Assert.False(File.Exists(temp.IniPath));
    }
}