using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using FiveMServerLauncher.Tests.Views;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher.Tests.ViewModels;

// The Servers page's own motion (detail slide-in, row hover/selection ease, busy pulse)
// runs only while the inherited motion switch is on.
public partial class MainViewModelTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ServersPage_SelectionChange_ShouldSlideTheDetailOnlyWithMotion(bool motion)
    {
        // Given the page with the switch set
        var (detailAnimated, rowAnimated, selectedOpacity) = RunOnSta(() =>
        {
            var (window, vm) = ShowServersPage("en");

            try
            {
                Motion.SetIsEnabled(window, motion);
                StaThread.Pump();
                var page = ServersPageOf(window);

                // When another server is selected
                vm.SelectedServer = vm.SavedServers[1];
                window.UpdateLayout();

                var detail = (FrameworkElement)page.FindName("DetailContent");
                var list = (ListBox)page.FindName("SavedServersList");
                var row = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(vm.SavedServers[1]);
                var layer = (FrameworkElement)row.Template.FindName("SelectedLayer", row);
                var animated = (detail.HasAnimatedProperties, layer.HasAnimatedProperties);
                StaThread.Pump(400);
                return (animated.Item1, animated.Item2, layer.Opacity);
            }
            finally
            {
                window.Close();
            }
        });

        // Then the detail and the row ease in with motion and snap without it; the row
        // ends fully selected either way
        Assert.Equal(motion, detailAnimated);
        Assert.Equal(motion, rowAnimated);
        Assert.Equal(1, selectedOpacity);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ServersPage_BusyDot_ShouldPulseOnlyWithMotion(bool motion)
    {
        // Given the page's busy dot bound to a busy view model
        var (visible, animated) = RunOnSta(() =>
        {
            var (window, _) = ShowServersPage("en");

            try
            {
                var dot = new Ellipse
                {
                    Style = (Style)ServersPageOf(window).FindResource("BusyDotStyle"),
                    DataContext = new { IsBusy = true }
                };
                Motion.SetIsEnabled(dot, motion);
                using var host = new OffscreenWindow(dot);
                StaThread.Pump();
                return (dot.Visibility == Visibility.Visible, dot.HasAnimatedProperties);
            }
            finally
            {
                window.Close();
            }
        });

        // Then it shows while busy and pulses only with motion on
        Assert.True(visible);
        Assert.Equal(motion, animated);
    }

    private static ServersPage ServersPageOf(Window window) =>
        (ServersPage)((MainView)window.FindName("ShellContent")).FindName("ServersPage");

    private sealed class OffscreenWindow : IDisposable
    {
        private readonly Window _window;

        public OffscreenWindow(FrameworkElement content)
        {
            _window = new Window
            {
                Content = content, Left = -10000, Top = -10000, Width = 100, Height = 100,
                ShowInTaskbar = false, ShowActivated = false
            };
            _window.Show();
        }

        public void Dispose() => _window.Close();
    }
}
