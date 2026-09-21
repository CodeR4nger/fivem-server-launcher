using System.Diagnostics;
using FiveMServerLauncher.Launch;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeProcessStarter : IProcessStarter
{
    public List<ProcessStartInfo> Starts { get; } = [];

    public void Start(ProcessStartInfo startInfo) => Starts.Add(startInfo);
}