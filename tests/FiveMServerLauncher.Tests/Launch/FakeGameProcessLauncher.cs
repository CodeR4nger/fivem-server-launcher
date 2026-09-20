using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeGameProcessLauncher : IGameProcessLauncher
{
    public List<Uri> Requests { get; } = [];

    public Task StartAsync(Uri uri)
    {
        Requests.Add(uri);
        return Task.CompletedTask;
    }
}