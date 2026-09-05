namespace FiveMServerLauncher.Configuration;

public class ConfigurationRepository
{
    private readonly ISettingsStorage _storage;

    public ConfigurationRepository(ISettingsStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);

        _storage = storage;
    }

    public void Save(LauncherSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _storage.Save(settings);
    }

    public LauncherSettings Load()
    {
        return _storage.Load() ?? new LauncherSettings();
    }
}
