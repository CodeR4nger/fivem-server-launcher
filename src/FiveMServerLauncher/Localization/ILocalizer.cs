namespace FiveMServerLauncher.Localization;

public interface ILocalizer
{
    string Language { get; }

    IReadOnlyList<LanguageOption> Languages { get; }

    event EventHandler? LanguageChanged;

    void SetLanguage(string? languageTag);

    string Get(string key);

    string Format(string key, params object[] args);
}
