using FiveMServerLauncher.Service;
using System.IO;

namespace FiveMServerLauncher.Tests.Launch;

internal sealed class FakeCitizenFxConfigWriter : ICitizenFxConfigWriter
{
    public List<(string IniPath, IReadOnlyDictionary<string, string> Values)> Calls { get; } = [];

    public bool Throw { get; set; }

    public Task ApplyAsync(string iniPath, IReadOnlyDictionary<string, string> values)
    {
        if (Throw)
        {
            throw new IOException("Simulated writer failure");
        }

        Calls.Add((iniPath, values));
        return Task.CompletedTask;
    }
}