using FiveMServerLauncher.Configuration;

namespace FiveMServerLauncher.Tests.Configuration;

public class InMemorySettingsStorage : ISettingsStorage
{
    private LauncherSettings? _settings;

    public int SaveCount { get; private set; }

    public void Save(LauncherSettings settings)
    {
        _settings = settings;
        SaveCount++;
    }

    public LauncherSettings? Load()
    {
        return _settings;
    }
}
