using System.Windows;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class RoundedBorderTests
{
    private static CornerRadius Arranged(CornerRadius requested, double width, double height) =>
        StaThread.Run(() =>
        {
            var border = new RoundedBorder { CornerRadius = requested };
            border.Measure(new Size(width, height));
            border.Arrange(new Rect(0, 0, width, height));
            return border.CornerRadius;
        });

    [Fact]
    public void CornerRadius_WhenLargerThanHalfTheShortSide_ShouldClampToAPill()
    {
        // Given a "fully rounded" request on a 120x30 box
        // When / Then every corner is half the height: a true pill, not an ellipse
        Assert.Equal(new CornerRadius(15), Arranged(new CornerRadius(999), 120, 30));
    }

    [Fact]
    public void CornerRadius_OnATallBox_ShouldClampToHalfTheWidth()
    {
        // Given / When / Then
        Assert.Equal(new CornerRadius(10), Arranged(new CornerRadius(999), 20, 60));
    }

    [Fact]
    public void CornerRadius_WhenSmallerThanHalfTheShortSide_ShouldStayAsRequested()
    {
        // Given / When / Then
        Assert.Equal(new CornerRadius(12), Arranged(new CornerRadius(12), 200, 40));
    }

    [Fact]
    public void CornerRadius_ShouldClampEachCornerIndependently()
    {
        // Given / When / Then
        Assert.Equal(new CornerRadius(0, 0, 14, 14), Arranged(new CornerRadius(0, 0, 999, 14), 100, 28));
    }

    [Fact]
    public void CornerRadius_ShouldFollowAResize()
    {
        // Given a pill arranged at height 30
        var radius = StaThread.Run(() =>
        {
            var border = new RoundedBorder { CornerRadius = new CornerRadius(999) };
            border.Arrange(new Rect(0, 0, 120, 30));

            // When it is re-arranged at height 48
            border.Arrange(new Rect(0, 0, 120, 48));
            return border.CornerRadius;
        });

        // Then the pill follows the new height
        Assert.Equal(new CornerRadius(24), radius);
    }

    [Fact]
    public void RoundedBorder_ShouldHaveATrueParameterlessConstructor()
    {
        // Given / When / Then: XAML-instantiated
        Assert.NotNull(StaThread.Run(() => Activator.CreateInstance(typeof(RoundedBorder))));
    }
}
