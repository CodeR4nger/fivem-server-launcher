using FiveMServerLauncher.Configuration;

namespace FiveMServerLauncher.Tests.Configuration;

public class InMemorySettingsStorage : ISettingsStorage
{
    private LauncherSettings? _settings;

    public void Save(LauncherSettings settings)
    {
        _settings = settings;
    }

    public LauncherSettings? Load()
    {
        return _settings;
    }
}
