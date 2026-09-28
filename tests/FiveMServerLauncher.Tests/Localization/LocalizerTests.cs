using System.Globalization;
using FiveMServerLauncher.Localization;

namespace FiveMServerLauncher.Tests.Localization;

public class LocalizerTests
{
    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Dictionaries(
        params (string Tag, string Json)[] payloads)
    {
        return Localizer.ParseDictionaries(payloads);
    }

    private const string EnglishJson = """
        {
            "SettingsButton": "⚙ SETTINGS",
            "EnterServerButton": "ENTER SERVER",
            "StatusStartingApp": "Starting {0}..."
        }
        """;

    private const string SpanishJson = """
        {
            "SettingsButton": "⚙ AJUSTES",
            "EnterServerButton": "ENTRAR AL SERVIDOR",
            "StatusStartingApp": "Iniciando {0}..."
        }
        """;

    [Fact]
    public void Get_WhenSystemLanguageShipped_ShouldReturnLocalizedString()
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)),
            () => "es-ES");

        // When / Then
        Assert.Equal("⚙ AJUSTES", localizer.Get("SettingsButton"));
    }

    [Theory]
    [InlineData("es-ES", "es")]
    [InlineData("pt-BR", "pt")]
    [InlineData("zh-CN", "zh-Hans")]
    public void Get_WhenSystemCultureMapsToShippedTag_ShouldUseThatLanguage(string culture, string expectedTag)
    {
        // Given
        var json = $$"""{ "SettingsButton": "{{expectedTag}}" }""";
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), (expectedTag, json)),
            () => culture);

        // When / Then
        Assert.Equal(expectedTag, localizer.Language);
    }

    [Theory]
    [InlineData("xx-XX")]
    [InlineData("")]
    [InlineData(null)]
    public void Get_WhenSystemLanguageNotShipped_ShouldFallBackToEnglish(string? culture)
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)),
            () => culture);

        // When / Then
        Assert.Equal("en", localizer.Language);
        Assert.Equal("ENTER SERVER", localizer.Get("EnterServerButton"));
    }

    [Fact]
    public void Get_WhenKeyMissingInLanguage_ShouldFallBackToEnglish()
    {
        // Given
        var partialSpanish = """{ "SettingsButton": "⚙ AJUSTES" }""";
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", partialSpanish)),
            () => "es-ES");

        // When / Then
        Assert.Equal("ENTER SERVER", localizer.Get("EnterServerButton"));
    }

    [Fact]
    public void Get_WhenKeyMissingEverywhere_ShouldReturnKey()
    {
        // Given
        var localizer = new Localizer(Dictionaries(("en", EnglishJson)));

        // When / Then
        Assert.Equal("NoSuchKey", localizer.Get("NoSuchKey"));
    }

    [Fact]
    public void Format_WithArguments_ShouldInterpolateInCurrentLanguage()
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)),
            () => "es-ES");

        // When / Then
        Assert.Equal("Iniciando Steam...", localizer.Format("StatusStartingApp", "Steam"));
    }

    [Fact]
    public void SetLanguage_WhenTagShipped_ShouldSwitchAndRaiseEvent()
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)),
            () => "en-US");
        var raised = 0;
        localizer.LanguageChanged += (_, _) => raised++;

        // When
        localizer.SetLanguage("es");

        // Then
        Assert.Equal("es", localizer.Language);
        Assert.Equal("⚙ AJUSTES", localizer.Get("SettingsButton"));
        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetLanguage_WhenNull_ShouldFollowSystemLanguageAgain()
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)),
            () => "es-ES");
        localizer.SetLanguage("en");

        // When
        localizer.SetLanguage(null);

        // Then
        Assert.Equal("es", localizer.Language);
    }

    [Fact]
    public void SetLanguage_WhenSameEffectiveLanguage_ShouldNotRaiseEvent()
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)),
            () => "es-ES");
        var raised = 0;
        localizer.LanguageChanged += (_, _) => raised++;

        // When
        localizer.SetLanguage("es");

        // Then
        Assert.Equal(0, raised);
    }

    [Fact]
    public void SetLanguage_WhenTagNotShipped_ShouldFallBackToSystemLanguage()
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)),
            () => "es-ES");

        // When
        localizer.SetLanguage("fr");

        // Then
        Assert.Equal("es", localizer.Language);
    }

    [Fact]
    public void Languages_ShouldListOnlyLanguagesWithDictionaries()
    {
        // Given
        var localizer = new Localizer(
            Dictionaries(("en", EnglishJson), ("es", SpanishJson)));

        // When / Then
        Assert.Equal(
            ["English", "Español"],
            localizer.Languages.Select(l => l.DisplayName).ToArray());
        Assert.Equal("en", localizer.Languages[0].Tag);
        Assert.Equal("es", localizer.Languages[1].Tag);
    }

    [Fact]
    public void ParseDictionaries_WhenJsonCorrupt_ShouldSkipThatLanguage()
    {
        // Given / When
        var dictionaries = Localizer.ParseDictionaries(
            ("en", EnglishJson),
            ("es", "{ not valid json"));

        // Then
        Assert.True(dictionaries.ContainsKey("en"));
        Assert.False(dictionaries.ContainsKey("es"));
    }

    [Fact]
    public void ParseDictionaries_WhenJsonEmpty_ShouldSkipThatLanguage()
    {
        // Given / When
        var dictionaries = Localizer.ParseDictionaries(("en", EnglishJson), ("es", ""));

        // Then
        Assert.False(dictionaries.ContainsKey("es"));
    }

    [Fact]
    public void FromEmbeddedResources_ShouldLoadEnglishDictionary()
    {
        // Given / When
        var localizer = Localizer.FromEmbeddedResources();

        // Then
        Assert.Equal("en", localizer.Language);
        Assert.Equal("⚙ SETTINGS", localizer.Get("SettingsButton"));
        Assert.Equal("ENTER SERVER", localizer.Get("EnterServerButton"));
        Assert.Contains(localizer.Languages, l => l.Tag == "en");
    }

    [Fact]
    public void LoadDictionaries_WhenLanguagesShipped_ShouldCoverEveryEnglishKey()
    {
        // Given — the completeness audit: every language file that ships must carry
        // every English key, or the English fallback would leak visibly.
        var dictionaries = Localizer.LoadDictionaries();

        // When
        var english = dictionaries["en"];
        var otherTags = dictionaries.Keys.Where(t => t != "en");

        // Then
        Assert.NotEmpty(english);
        Assert.All(otherTags, tag =>
        {
            var missing = english.Keys.Where(key => !dictionaries[tag].ContainsKey(key)).ToList();
            Assert.True(missing.Count == 0, $"Language '{tag}' is missing keys: {string.Join(", ", missing)}");
        });
    }
}
