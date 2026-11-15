using System.Windows;
using System.Windows.Media;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

// The theme's tokens: every text/background pair the styles use reaches WCAG 4.5:1, and
// the bundled Inter resolves a real face for every weight the ramp uses.
public class ThemeTokenTests
{
    // Every text colour the theme's styles draw, on every background it is drawn on.
    public static TheoryData<string, string> TextPairs => new()
    {
        { "TextPrimaryBrush", "GroundBrush" },
        { "TextPrimaryBrush", "TitleBarBrush" },
        { "TextPrimaryBrush", "PanelBrush" },
        { "TextPrimaryBrush", "SurfaceRaisedBrush" },
        { "TextPrimaryBrush", "SurfaceHoverBrush" },
        { "TextPrimaryBrush", "SettingsGroupBrush" },
        { "TextPrimaryBrush", "InputBrush" },
        { "TextSecondaryBrush", "GroundBrush" },
        { "TextSecondaryBrush", "PanelBrush" },
        { "TextSecondaryBrush", "SurfaceRaisedBrush" },
        { "TextSecondaryBrush", "SurfaceHoverBrush" },
        { "TextSecondaryBrush", "SettingsGroupBrush" },
        { "TextSecondaryBrush", "InputBrush" },
        { "TextTertiaryBrush", "PanelBrush" },
        { "TextTertiaryBrush", "SurfaceRaisedBrush" },
        { "TextTertiaryBrush", "SurfaceHoverBrush" },
        { "TextTertiaryBrush", "SettingsGroupBrush" },
        { "TextTertiaryBrush", "InputBrush" },
        { "DangerTextBrush", "GroundBrush" },
        { "DangerTextBrush", "PanelBrush" },
        { "DangerTextBrush", "SurfaceRaisedBrush" },
        { "DangerTextBrush", "InputBrush" },
        { "FooterTextBrush", "FooterBrush" },
        { "OnAccentBrush", "AccentBrush" },
        { "OnSelectedBrush", "TextPrimaryBrush" },
    };

    [Theory]
    [MemberData(nameof(TextPairs))]
    public void TextPair_ShouldReachNormalTextContrast(string foreground, string background)
    {
        // Given the two theme colours
        var ratio = StaThread.Run(() =>
        {
            var theme = new ThemeResources();
            return Contrast.Ratio(((SolidColorBrush)theme[foreground]).Color, ((SolidColorBrush)theme[background]).Color);
        });

        // Then the pair is readable as normal text
        Assert.True(ratio >= 4.5, $"{foreground} on {background}: {ratio:F2}:1");
    }

    [Theory]
    [InlineData("#F5A623")]
    [InlineData("#4DA3FF")]
    [InlineData("#E05252")]
    public void GameTagText_ShouldReachNormalTextContrastOnEveryTagFill(string fill)
    {
        // Given the tag text token and a game tag fill
        var ratio = StaThread.Run(() =>
            Contrast.Ratio(
                ((SolidColorBrush)new ThemeResources()["GameTagTextBrush"]).Color,
                (Color)ColorConverter.ConvertFromString(fill)));

        // Then
        Assert.True(ratio >= 4.5, $"{fill}: {ratio:F2}:1");
    }

    [Theory]
    [InlineData(400)]
    [InlineData(500)]
    [InlineData(600)]
    [InlineData(700)]
    [InlineData(800)]
    public void UiFontFamily_ShouldResolveTheBundledInterFaceForTheWeight(int weight)
    {
        // Given the theme's font family (bundled Inter, Segoe UI fallback)
        var (resolved, file, simulated) = StaThread.Run(() =>
        {
            var family = (FontFamily)new ThemeResources()["UiFontFamily"];
            var typeface = new Typeface(family, FontStyles.Normal, FontWeight.FromOpenTypeWeight(weight), FontStretches.Normal);

            // When the weight is resolved to a face
            var found = typeface.TryGetGlyphTypeface(out var glyphs);
            return (found, glyphs?.FontUri.ToString() ?? string.Empty, typeface.IsBoldSimulated);
        });

        // Then it is a real Inter face (not the fallback, not a synthesized bold)
        Assert.True(resolved);
        Assert.Contains("Inter-", file, StringComparison.OrdinalIgnoreCase);
        Assert.False(simulated);
    }
}
