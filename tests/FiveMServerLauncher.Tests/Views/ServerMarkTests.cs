using System.Globalization;
using System.Windows.Media;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class ServerMarkTests
{
    private static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    [Theory]
    [InlineData("Los Santos Roleplay", "LS")]
    [InlineData("wild west frontier", "WW")]
    [InlineData("Elysium", "E")]
    [InlineData("[EU] Wild West", "EW")]
    [InlineData("  ★★ Drift   Club ★★ ", "DC")]
    [InlineData("Сервер Россия", "СР")]
    [InlineData("1st Division", "1D")]
    [InlineData("O'Neil Island", "OI")]
    public void Initials_ShouldTakeTheFirstLetterOfTheFirstTwoWords(string name, string expected)
    {
        // Given / When / Then
        Assert.Equal(expected, ServerMark.Initials(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("★ — ★")]
    public void Initials_WhenNoLetterOrDigit_ShouldReturnQuestionMark(string? name)
    {
        // Given / When / Then
        Assert.Equal("?", ServerMark.Initials(name));
    }

    [Fact]
    public void Initials_WhenTheLetterIsOutsideTheBasicPlane_ShouldKeepTheWholeCharacter()
    {
        // Given a mathematical bold capital A (a surrogate pair) starting the name
        var name = "\U0001D400lpha Base";

        // When
        var initials = ServerMark.Initials(name);

        // Then the pair is never split
        Assert.Equal("\U0001D400B", initials);
    }

    [Fact]
    public void Color_ShouldBeDeterministicAndCaseInsensitive()
    {
        // Given / When
        var first = ServerMark.Color("Los Santos Roleplay");
        var again = ServerMark.Color("Los Santos Roleplay");
        var upper = ServerMark.Color("LOS SANTOS ROLEPLAY");

        // Then
        Assert.Equal(first, again);
        Assert.Equal(first, upper);
    }

    [Fact]
    public void Color_ShouldBeStableAcrossRuns()
    {
        // Given pinned names (expected values from an independent FNV-1a over the upper-cased
        // UTF-8 name): a process-randomized hash (string.GetHashCode) would move them
        // When / Then
        Assert.Equal("#AA4469", Hex(ServerMark.Color("Los Santos Roleplay")));
        Assert.Equal("#5858BC", Hex(ServerMark.Color("Wild West Frontier")));
    }

    [Fact]
    public void Color_ShouldIgnoreSurroundingWhitespace()
    {
        // Given / When / Then
        Assert.Equal(ServerMark.Color("Elysium"), ServerMark.Color("  Elysium  "));
    }

    [Fact]
    public void Color_ShouldSpreadNamesAcrossThePalette()
    {
        // Given
        var names = Enumerable.Range(0, 64).Select(i => $"Server {i}");

        // When
        var distinct = names.Select(ServerMark.Color).Distinct().Count();

        // Then
        Assert.Equal(ServerMark.Palette.Count, distinct);
    }

    [Fact]
    public void Color_WhenNameIsNullOrBlank_ShouldReturnTheFirstPaletteColor()
    {
        // Given / When / Then
        Assert.Equal(ServerMark.Palette[0], ServerMark.Color(null));
        Assert.Equal(ServerMark.Palette[0], ServerMark.Color("  "));
    }

    [Fact]
    public void Palette_EveryColorShouldReachTextContrastWithTheInitials()
    {
        // Given the initials are drawn in the primary text colour
        var text = (Color)ColorConverter.ConvertFromString("#F2F2F2");

        // When / Then
        Assert.All(ServerMark.Palette, color =>
            Assert.True(Contrast.Ratio(text, color) >= 4.5, $"{Hex(color)}: {Contrast.Ratio(text, color):F2}"));
    }

    [Fact]
    public void InitialsConverter_ShouldConvertTheName()
    {
        // Given
        var converter = new ServerMarkInitialsConverter();

        // When
        var result = converter.Convert("Los Santos Roleplay", typeof(string), null!, CultureInfo.InvariantCulture);

        // Then
        Assert.Equal("LS", result);
    }

    [Fact]
    public void BrushConverter_ShouldReturnAFrozenBrushOfTheMarkColor()
    {
        // Given
        var converter = new ServerMarkBrushConverter();

        // When
        var brush = (SolidColorBrush)converter.Convert("Wild West Frontier", typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Then
        Assert.True(brush.IsFrozen);
        Assert.Equal(ServerMark.Color("Wild West Frontier"), brush.Color);
    }

    [Theory]
    [InlineData(typeof(ServerMarkInitialsConverter))]
    [InlineData(typeof(ServerMarkBrushConverter))]
    public void Converters_ShouldHaveATrueParameterlessConstructor(Type type)
    {
        // Given / When / Then: XAML-instantiated, BAML needs it
        Assert.NotNull(Activator.CreateInstance(type));
    }
}
