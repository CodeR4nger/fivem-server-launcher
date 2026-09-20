namespace FiveMServerLauncher.Launch;

public interface IGameProcessLauncher
{
    Task StartAsync(Uri uri);
}