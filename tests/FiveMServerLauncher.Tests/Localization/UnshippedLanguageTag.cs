using FiveMServerLauncher.Localization;

namespace FiveMServerLauncher.Tests.Localization;

/// <summary>
/// Supplies a real culture the product does not ship, for tests that assert a
/// language is rejected. Derived from the declared shipped set rather than
/// hardcoded, because a hardcoded tag silently changes meaning the day that
/// language ships — that is how "fr" sat in two tests as a stand-in for
/// "not shipped" while French was very much shipped.
/// </summary>
internal static class UnshippedLanguageTag
{
    private static readonly string[] Candidates =
    [
        "nl", "sv", "da", "fi", "nb", "cs", "hu", "el", "tr", "uk",
        "he", "ar", "th", "vi", "id", "ro", "bg", "hr", "sk", "sl",
        "et", "lv", "lt"
    ];

    internal static string Value =>
        Candidates.FirstOrDefault(tag => !IsShipped(tag))
        ?? throw new InvalidOperationException(
            "Every unshipped-culture candidate is now shipped. Add a new one to " +
            $"{nameof(UnshippedLanguageTag)}.{nameof(Candidates)}.");

    internal static bool IsShipped(string tag)
    {
        return Localizer.ShippedLanguageOptions.Any(option => option.Tag == tag);
    }
}
