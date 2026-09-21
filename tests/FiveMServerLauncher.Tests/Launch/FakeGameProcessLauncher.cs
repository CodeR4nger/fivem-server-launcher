using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeGameProcessLauncher : IGameProcessLauncher
{
    public List<Uri> Requests { get; } = [];
    public List<string> ExecutableStarts { get; } = [];
    public List<(string Path, IReadOnlyList<string> Args)> ExecutableArgsStarts { get; } = [];
    public bool ThrowOnStart { get; set; }
    public bool ThrowOnExecutableStart { get; set; }
    public Task? ExecutableStartBarrier { get; set; }

    public Task StartAsync(Uri uri)
    {
        if (ThrowOnStart)
        {
            throw new System.ComponentModel.Win32Exception("No application is associated with the specified file.");
        }

        Requests.Add(uri);
        return Task.CompletedTask;
    }

    public async Task StartExecutableAsync(string executablePath)
    {
        if (ThrowOnExecutableStart)
        {
            throw new System.ComponentModel.Win32Exception("Simulated executable start failure");
        }

        ExecutableStarts.Add(executablePath);

        if (ExecutableStartBarrier is not null)
        {
            await ExecutableStartBarrier;
        }
    }

    public async Task StartExecutableAsync(string executablePath, IReadOnlyList<string> arguments)
    {
        if (ThrowOnExecutableStart)
        {
            throw new System.ComponentModel.Win32Exception("Simulated executable start failure");
        }

        ExecutableArgsStarts.Add((executablePath, arguments));

        if (ExecutableStartBarrier is not null)
        {
            await ExecutableStartBarrier;
        }
    }
}