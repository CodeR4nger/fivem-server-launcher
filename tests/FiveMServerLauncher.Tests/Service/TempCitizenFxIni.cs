using System;
using System.IO;

namespace FiveMServerLauncher.Tests.Service;

internal sealed class TempCitizenFxIni : IDisposable
{
    public TempCitizenFxIni()
    {
        DirectoryPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString());

        IniPath = Path.Combine(DirectoryPath, "CitizenFX.ini");
    }

    public string DirectoryPath { get; }

    public string IniPath { get; }

    public void Dispose()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            return;
        }

        if (File.Exists(IniPath))
        {
            File.SetAttributes(IniPath, FileAttributes.Normal);
        }

        Directory.Delete(DirectoryPath, true);
    }
}