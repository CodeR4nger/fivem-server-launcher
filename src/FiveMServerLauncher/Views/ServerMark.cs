using System.Text;
using System.Windows.Media;

namespace FiveMServerLauncher.Views;

// The server mark's fallback when a server has no icon: initials and a colour derived
// from the name. Pure and deterministic — the colour hash is FNV-1a over the trimmed,
// upper-cased UTF-8 name, so it is stable across runs (string.GetHashCode is randomized
// per process). Every palette colour reaches 4.5:1 against the #F2F2F2 initials.
public static class ServerMark
{
    private const string NoInitials = "?";

    public static IReadOnlyList<Color> Palette { get; } =
    [
        Rgb(0xA9, 0x50, 0x2A),
        Rgb(0x86, 0x66, 0x1C),
        Rgb(0x3B, 0x76, 0x3D),
        Rgb(0x2A, 0x76, 0x70),
        Rgb(0x2F, 0x6B, 0xAD),
        Rgb(0x58, 0x58, 0xBC),
        Rgb(0x8B, 0x4C, 0xAD),
        Rgb(0xAA, 0x44, 0x69),
    ];

    // The first letter (or digit) of each of the first two words. A word is a run of
    // letters/digits; apostrophes stay inside it ("O'Neil"), everything else separates.
    // Whole runes, so a letter outside the basic plane is never split.
    public static string Initials(string? name)
    {
        var initials = new StringBuilder();
        var count = 0;
        var inWord = false;

        foreach (var rune in (name ?? string.Empty).EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                if (!inWord)
                {
                    initials.Append(Rune.ToUpperInvariant(rune).ToString());
                    inWord = true;

                    if (++count == 2)
                    {
                        break;
                    }
                }
            }
            else if (!IsApostrophe(rune) || !inWord)
            {
                inWord = false;
            }
        }

        return initials.Length == 0 ? NoInitials : initials.ToString();
    }

    public static Color Color(string? name) => Palette[IndexOf(name)];

    // Frozen, shared: one brush per palette colour instead of one per bound row.
    public static SolidColorBrush Brush(string? name) => PaletteBrushes[IndexOf(name)];

    private static readonly SolidColorBrush[] PaletteBrushes = Palette.Select(Freeze).ToArray();

    private static int IndexOf(string? name)
    {
        var key = (name ?? string.Empty).Trim().ToUpperInvariant();
        var hash = 0x811C9DC5u;

        foreach (var b in Encoding.UTF8.GetBytes(key))
        {
            hash = (hash ^ b) * 0x01000193u;
        }

        return key.Length == 0 ? 0 : (int)(hash % (uint)Palette.Count);
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static bool IsApostrophe(Rune rune) => rune.Value is '\'' or '\u2019';

    private static Color Rgb(byte r, byte g, byte b) => System.Windows.Media.Color.FromRgb(r, g, b);
}
