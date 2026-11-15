using System.Windows;
using System.Windows.Controls;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.Views;

public class MotionTests
{
    [Fact]
    public void IsEnabled_ShouldDefaultToOffUntilTheRootOptsIn()
    {
        // Given an element outside any motion-enabled root (or before the window's binding
        // resolves, while BAML is still being parsed)
        // When / Then motion is off, so no theme storyboard can start early
        var enabled = StaThread.Run(() => Motion.GetIsEnabled(new Border()));

        Assert.False(enabled);
    }

    [Fact]
    public void IsEnabled_ShouldInheritFromTheRootDownTheTree()
    {
        // Given a root with motion switched on
        var inherited = StaThread.Run(() =>
        {
            var leaf = new Border();
            var root = new Grid();
            root.Children.Add(new StackPanel { Children = { leaf } });

            // When
            Motion.SetIsEnabled(root, true);

            // Then the leaf reads the root's switch
            return Motion.GetIsEnabled(leaf);
        });

        Assert.True(inherited);
    }
}
