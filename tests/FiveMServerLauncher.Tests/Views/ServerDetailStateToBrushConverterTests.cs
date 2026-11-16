using System.Globalization;
using System.Windows.Media;
using FiveMServerLauncher.ViewModels;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

// The Servers page's status dot: listed green, manual amber, offline and not listed grey
// (red stays for RedM and errors).
public class ServerDetailStateToBrushConverterTests
{
    [Theory]
    [InlineData(ServerDetailState.Listed, "#FF3FB950")]
    [InlineData(ServerDetailState.Manual, "#FFD29922")]
    [InlineData(ServerDetailState.Offline, "#FF7A7A7A")]
    [InlineData(ServerDetailState.NotListed, "#FF7A7A7A")]
    public void Convert_ShouldColourEachState(ServerDetailState state, string expected)
    {
        // Given / When
        var brush = (SolidColorBrush)new ServerDetailStateToBrushConverter().Convert(
            state, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Then
        Assert.Equal(expected, brush.Color.ToString());
        Assert.True(brush.IsFrozen);
    }

    [Fact]
    public void Convert_WithNoState_ShouldBeGrey()
    {
        // Given / When — no server selected
        var brush = (SolidColorBrush)new ServerDetailStateToBrushConverter().Convert(
            null!, typeof(Brush), null!, CultureInfo.InvariantCulture);

        // Then
        Assert.Equal("#FF7A7A7A", brush.Color.ToString());
    }

    [Fact]
    public void Ctor_ShouldBeTrulyParameterless()
    {
        // Given / When / Then: XAML-instantiated
        Assert.NotNull(Activator.CreateInstance(typeof(ServerDetailStateToBrushConverter)));
    }
}
