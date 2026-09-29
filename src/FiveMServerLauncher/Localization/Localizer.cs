using System.Globalization;
using System.IO;
using System.Text.Json;

namespace FiveMServerLauncher.Localization;

public sealed class Localizer : ILocalizer
{
    public const string FallbackTag = "en";

    private const string ResourcePrefix = "FiveMServerLauncher.Localization.";

    private const string DisplayNameKey = "_displayName";

    // Keys beginning with an underscore describe the language file (its native
    // name, its review status); they are metadata, never UI strings. Folding them
    // into the string set would make the English key set require a `_note` that
    // en.json has no reason to carry.
    private const char MetadataKeyPrefix = '_';

    // Tags come from the embedded resource NAMES, not from the payloads: a file
    // that fails to parse still contributes its tag, so the language stays
    // selectable (rendering through the English fallback) and the completeness
    // audit can still fail the build on it. Deriving tags from parsed content
    // would silently shrink the shipped set instead.
    // Ordered explicitly because resource enumeration order is not stable across
    // builds: the fallback language leads, the rest follow by tag.
    public static IReadOnlyList<LanguageOption> ShippedLanguageOptions { get; } =
        ReadEmbeddedJsonPayloads()
            .Select(payload => ReadLanguageOption(payload.Tag, payload.Json))
            .OrderBy(option => option.Tag == FallbackTag ? 0 : 1)
            .ThenBy(option => option.Tag, StringComparer.Ordinal)
            .ToArray();

    private static readonly HashSet<string> ShippedTags =
        ShippedLanguageOptions.Select(l => l.Tag).ToHashSet();

    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _dictionaries;
    private readonly Func<string?> _systemCultureProvider;
    private readonly object _stateGate = new();

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
            var strings = ParseStrings(json);

            if (strings.Count > 0)
            {
                dictionaries[tag] = strings;
            }
        }

        return dictionaries;
    }

    /// <summary>
    /// Reads a language file's picker label. The native name is data, so adding a
    /// language needs no code change; a file that omits it, or fails to parse,
    /// falls back to the tag rather than throwing.
    /// </summary>
    public static LanguageOption ReadLanguageOption(string tag, string json)
    {
        if (ParseEntries(json).TryGetValue(DisplayNameKey, out var displayName)
            && !string.IsNullOrWhiteSpace(displayName))
        {
            return new LanguageOption(tag, displayName);
        }

        return new LanguageOption(tag, tag);
    }

    private static Dictionary<string, string> ParseStrings(string json)
    {
        return ParseEntries(json)
            .Where(entry => !entry.Key.StartsWith(MetadataKeyPrefix))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
    }

    private static Dictionary<string, string> ParseEntries(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            // A corrupt language file degrades to the fallback language.
            return [];
        }
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

    // A shipped language stays selectable even when its file failed to parse: the
    // English fallback renders it (see Get), which beats silently dropping the
    // user's explicit choice and reverting them to their system language.
    public IReadOnlyList<LanguageOption> Languages => ShippedLanguageOptions;

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
            var effective = requested is not null && ShippedTags.Contains(requested)
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
            if (ShippedTags.Contains(candidate))
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
