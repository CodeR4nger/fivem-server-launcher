using FiveMServerLauncher.Localization;

namespace FiveMServerLauncher.Tests.Localization;

internal static class TestLocalizer
{
    /// <summary>
    /// The real shipped English dictionaries, for tests that assert an English
    /// label but are not exercising the language machinery itself.
    /// </summary>
    internal static ILocalizer English()
    {
        return Localizer.FromEmbeddedResources(() => "en-US");
    }

    /// <summary>
    /// A localizer over caller-supplied dictionaries, for tests that need a
    /// specific language switch.
    /// </summary>
    internal static ILocalizer For(
        string systemCulture = "en-US",
        params (string Tag, string Json)[] payloads)
    {
        return new Localizer(Localizer.ParseDictionaries(payloads), () => systemCulture);
    }
}
