using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeGameProcessLauncher : IGameProcessLauncher
{
    public List<Uri> Requests { get; } = [];
    public bool ThrowOnStart { get; set; }

    public Task StartAsync(Uri uri)
    {
        if (ThrowOnStart)
        {
            throw new System.ComponentModel.Win32Exception("No application is associated with the specified file.");
        }

        Requests.Add(uri);
        return Task.CompletedTask;
    }
}