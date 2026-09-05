namespace FiveMServerLauncher.Configuration;

public class ConfigurationRepository
{
    private LauncherSettings? _settings;

    public void Save(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
    }

    public LauncherSettings Load()
    {
        return _settings ?? new LauncherSettings();
    }
}
