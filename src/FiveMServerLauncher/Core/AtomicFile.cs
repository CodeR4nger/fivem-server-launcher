using System.IO;

namespace FiveMServerLauncher.Core;

public static class AtomicFile
{
    public static void WriteAllText(string path, string content)
    {
        Write(path, temp => File.WriteAllText(temp, content));
    }

    public static void WriteAllBytes(string path, byte[] content)
    {
        Write(path, temp => File.WriteAllBytes(temp, content));
    }

    private static void Write(string path, Action<string> writeTemp)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temp = $"{path}.tmp-{Guid.NewGuid():N}";
        writeTemp(temp);

        if (File.Exists(path))
        {
            File.Replace(temp, path, null);
        }
        else
        {
            File.Move(temp, path);
        }
    }
}
