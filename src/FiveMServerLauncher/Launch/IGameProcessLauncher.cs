namespace FiveMServerLauncher.Launch;

public interface IGameProcessLauncher
{
    Task StartAsync(Uri uri);

    Task StartExecutableAsync(string executablePath);

    Task StartExecutableAsync(string executablePath, IReadOnlyList<string> arguments);
}