namespace FiveMServerLauncher.Localization;

public static class DefaultLocalizer
{
    private static readonly Lazy<ILocalizer> Instance = new(() => Localizer.FromEmbeddedResources());

    public static ILocalizer Get()
    {
        return Instance.Value;
    }
}
