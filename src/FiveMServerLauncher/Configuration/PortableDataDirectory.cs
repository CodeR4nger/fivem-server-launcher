using System.IO;

namespace FiveMServerLauncher.Configuration;

public sealed class PortableDataDirectory
{
    private readonly Func<string?> _processPathProvider;

    public PortableDataDirectory(Func<string?> processPathProvider)
    {
        ArgumentNullException.ThrowIfNull(processPathProvider);

        _processPathProvider = processPathProvider;
    }

    public static PortableDataDirectory Default()
    {
        return new PortableDataDirectory(() => Environment.ProcessPath);
    }

    public string SettingsPath => Path.Combine(DataDirectory, "launcher-settings.json");

    public string ServersPath => Path.Combine(DataDirectory, "saved-servers.json");

    public string DataDirectory
    {
        get
        {
            var processPath = _processPathProvider();

            if (string.IsNullOrWhiteSpace(processPath))
            {
                throw new InvalidOperationException(
                    "The launcher executable path could not be resolved; cannot determine a portable data directory.");
            }

            return Path.GetDirectoryName(Path.GetFullPath(processPath))!;
        }
    }
}