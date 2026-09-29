using System.Globalization;
using System.IO;
using System.Text.Json;

namespace FiveMServerLauncher.Localization;

public sealed class Localizer : ILocalizer
{
    public const string FallbackTag = "en";

    private const string ResourcePrefix = "FiveMServerLauncher.Localization.";

    private static readonly LanguageOption[] ShippedLanguages =
    [
        new("en", "English"),
        new("es", "Español"),
        new("fr", "Français"),
        new("de", "Deutsch"),
        new("it", "Italiano"),
        new("ja", "日本語"),
        new("ko", "한국어"),
        new("pl", "Polski"),
        new("pt", "Português"),
        new("ru", "Русский"),
        new("zh-Hans", "简体中文")
    ];

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _dictionaries;
    private readonly Func<string?> _systemCultureProvider;
    private readonly object _stateGate = new();

    private string? _requestedTag;
    private string _effectiveTag;

    public Localizer(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> dictionaries,
        Func<string?>? systemCultureProvider = null)
    {
        _dictionaries = dictionaries;
        _systemCultureProvider = systemCultureProvider ?? DefaultSystemCulture;
        _effectiveTag = ResolveSystemLanguage();
    }

    public static Localizer FromEmbeddedResources(Func<string?>? systemCultureProvider = null)
    {
        return new Localizer(LoadDictionaries(), systemCultureProvider);
    }

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadDictionaries()
    {
        return ParseDictionaries(ReadEmbeddedJsonPayloads().ToArray());
    }

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> ParseDictionaries(
        params (string Tag, string Json)[] payloads)
    {
        var dictionaries = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        foreach (var (tag, json) in payloads)
        {
            try
            {
                var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (entries is { Count: > 0 })
                {
                    dictionaries[tag] = entries;
                }
            }
            catch (JsonException)
            {
                // A corrupt language file degrades to the fallback language.
            }
        }

        return dictionaries;
    }

    private static IEnumerable<(string Tag, string Json)> ReadEmbeddedJsonPayloads()
    {
        var assembly = typeof(Localizer).Assembly;

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !name.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream!);

            yield return (name[ResourcePrefix.Length..^".json".Length], reader.ReadToEnd());
        }
    }

    private static string DefaultSystemCulture()
    {
        return CultureInfo.CurrentUICulture.Name;
    }

    public string Language
    {
        get
        {
            lock (_stateGate)
            {
                return _effectiveTag;
            }
        }
    }

    public IReadOnlyList<LanguageOption> Languages =>
        ShippedLanguages.Where(l => _dictionaries.ContainsKey(l.Tag)).ToArray();

    public event EventHandler? LanguageChanged;

    public void SetLanguage(string? languageTag)
    {
        var requested = string.IsNullOrWhiteSpace(languageTag) ? null : languageTag;

        // Resolved outside the gate: the injectable culture provider must never
        // deadlock against a reader (it runs even when the requested tag resolves
        // directly — a pure culture-name lookup, not observable behavior).
        var cultureName = _systemCultureProvider();

        lock (_stateGate)
        {
            _requestedTag = requested;

            var effective = requested is not null && _dictionaries.ContainsKey(requested)
                ? requested
                : MatchSystemLanguage(cultureName);

            if (effective == _effectiveTag)
            {
                return;
            }

            _effectiveTag = effective;
        }

        // Raised outside the gate: handlers must never deadlock against a Get call.
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public string Get(string key)
    {
        string tag;

        lock (_stateGate)
        {
            tag = _effectiveTag;
        }

        if (_dictionaries.TryGetValue(tag, out var language)
            && language.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_dictionaries.TryGetValue(FallbackTag, out var fallback)
            && fallback.TryGetValue(key, out var fallbackValue))
        {
            return fallbackValue;
        }

        return key;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    private string ResolveSystemLanguage()
    {
        return MatchSystemLanguage(_systemCultureProvider());
    }

    private string MatchSystemLanguage(string? cultureName)
    {
        if (string.IsNullOrEmpty(cultureName))
        {
            return FallbackTag;
        }

        foreach (var candidate in SystemLanguageCandidates(cultureName))
        {
            if (_dictionaries.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        return FallbackTag;
    }

    private static IEnumerable<string> SystemLanguageCandidates(string cultureName)
    {
        CultureInfo? culture = null;

        try
        {
            culture = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
        }

        if (culture is null)
        {
            yield return cultureName;
            yield break;
        }

        yield return culture.Name;

        var twoLetter = culture.TwoLetterISOLanguageName;
        yield return twoLetter;

        // The shipped Chinese variant is Simplified Chinese.
        if (twoLetter == "zh")
        {
            yield return "zh-Hans";
        }
    }
}
