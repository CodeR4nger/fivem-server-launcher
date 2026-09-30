namespace FiveMServerLauncher.Core;

public static class AppVersion
{
    public static Version? TryParseTag(string? tag)
    {
        if (tag is null)
        {
            return null;
        }

        var text = tag.StartsWith('v') ? tag[1..] : tag;

        if (Version.TryParse(text, out var version)
            && version.Build >= 0
            && version.Revision == -1)
        {
            return version;
        }

        return null;
    }
}
