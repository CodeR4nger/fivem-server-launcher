using System.Text.RegularExpressions;
using FiveMServerLauncher.Localization;

namespace FiveMServerLauncher.Tests.Localization;

// The orphan audit: a key nothing renders any more is dead weight in every shipped
// language, and translators keep paying for it. Each English key must be referenced by
// the app's sources — XAML through {loc:Loc Key}, code through the "Key" literal handed
// to ILocalizer.Get/Format.
public class LocalizationKeyUsageTests
{
    // Keys whose name is built at run time, so no source scan can find them. Each entry
    // needs a comment saying where the name is composed. Empty today: every key is a
    // literal somewhere.
    private static readonly string[] DynamicallyBuiltKeys = [];

    [Fact]
    public void EnglishKeys_ShouldEachBeReferencedByTheAppSources()
    {
        // Given
        var english = Localizer.LoadDictionaries()["en"];
        var sources = AppSources();

        // When
        var orphaned = english.Keys
            .Where(key => !DynamicallyBuiltKeys.Contains(key))
            .Where(key => !IsReferenced(key, sources))
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Then
        Assert.NotEmpty(sources);
        Assert.True(orphaned.Length == 0, $"Keys nothing references: {string.Join(", ", orphaned)}");
    }

    [Fact]
    public void DynamicallyBuiltKeys_ShouldAllStillExist()
    {
        // Given — an allowlist entry for a deleted key would hide nothing but rot.
        var english = Localizer.LoadDictionaries()["en"];

        // When / Then
        Assert.All(DynamicallyBuiltKeys, key => Assert.True(english.ContainsKey(key), key));
    }

    private static bool IsReferenced(string key, IReadOnlyList<string> sources)
    {
        var pattern = new Regex(
            $@"""{Regex.Escape(key)}""|\{{loc:Loc {Regex.Escape(key)}\}}",
            RegexOptions.CultureInvariant);
        return sources.Any(pattern.IsMatch);
    }

    private static IReadOnlyList<string> AppSources()
    {
        var root = Path.Combine(RepositoryRoot(), "src", "FiveMServerLauncher");
        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .Where(path => !IsBuildOutput(root, path))
            .Select(File.ReadAllText)
            .ToArray();
    }

    private static bool IsBuildOutput(string root, string path)
    {
        var first = Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar)[0];
        return first is "bin" or "obj";
    }

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FiveMServerLauncher.slnx")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("Repository root (FiveMServerLauncher.slnx) not found above the test output.");
    }
}
