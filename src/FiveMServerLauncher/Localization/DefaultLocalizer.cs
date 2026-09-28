namespace FiveMServerLauncher.Localization;

public static class DefaultLocalizer
{
    private static readonly Lazy<ILocalizer> Instance = new(
        () => Localizer.FromEmbeddedResources(() => "en-US"));

    public static ILocalizer Get()
    {
        return Instance.Value;
    }
}
