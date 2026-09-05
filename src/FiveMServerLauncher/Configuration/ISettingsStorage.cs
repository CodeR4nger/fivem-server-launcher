namespace FiveMServerLauncher.Configuration;

public interface ISettingsStorage
{
    void Save(LauncherSettings settings);

    LauncherSettings? Load();
}
