using System;
using System.IO;

namespace FiveMServerLauncher.Tests.Configuration;

internal sealed class TempSettingsDirectory : IDisposable
{
    public TempSettingsDirectory()
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        FilePath = Path.Combine(DirectoryPath, "settings.json");
    }

    public string DirectoryPath { get; }

    public string FilePath { get; }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, true);
        }
    }
}