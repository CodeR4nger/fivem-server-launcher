using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using FiveMServerLauncher.Configuration;
using FiveMServerLauncher.Views;

namespace FiveMServerLauncher
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StateChanged += (_, _) => UpdateMaximizedChrome();
            SizeChanged += (_, _) => UpdateMaximizedChrome();
            LocationChanged += (_, _) => UpdateMaximizedChrome();
            DpiChanged += (_, _) => UpdateMaximizedChrome();
            Loaded += (_, _) => UpdateMaximizedChrome();
        }

        /// <summary>Applies a restored placement; call before the window is first shown.</summary>
        public void ApplyPlacement(RestoredPlacement placement)
        {
            Width = placement.Width;
            Height = placement.Height;

            if (placement.Position is { } position)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = position.X;
                Top = position.Y;
            }

            if (placement.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        /// <summary>The placement to remember now (restoreState: what a minimized window restores to).</summary>
        public WindowPlacement CapturePlacement(WindowState restoreState) =>
            WindowPlacementRules.Capture(
                WindowState, restoreState, new Rect(Left, Top, ActualWidth, ActualHeight), RestoreBounds);

        // A maximized WindowChrome window keeps its (invisible) frame, so its client area
        // overhangs the monitor's work area by the frame thickness on every side: the edges
        // would be cut and the bottom would reach under the taskbar. The root grid is inset
        // by exactly that overhang, measured against the window's monitor work area.
        private void UpdateMaximizedChrome()
        {
            RootGrid.Margin = WindowState == WindowState.Maximized ? MaximizedOverhang() : new Thickness(0);
        }

        private Thickness MaximizedOverhang()
        {
            var handle = new WindowInteropHelper(this).Handle;

            if (handle == IntPtr.Zero || !GetWindowRect(handle, out var window))
            {
                return new Thickness(0);
            }

            var info = new MonitorWorkAreas.MonitorInfo { Size = Marshal.SizeOf<MonitorWorkAreas.MonitorInfo>() };

            if (!MonitorWorkAreas.GetMonitorInfo(MonitorFromWindow(handle, MonitorDefaultToNearest), ref info))
            {
                return new Thickness(0);
            }

            var work = info.Work;
            var dpi = VisualTreeHelper.GetDpi(this);

            return new Thickness(
                Math.Max(0, work.Left - window.Left) / dpi.DpiScaleX,
                Math.Max(0, work.Top - window.Top) / dpi.DpiScaleY,
                Math.Max(0, window.Right - work.Right) / dpi.DpiScaleX,
                Math.Max(0, window.Bottom - work.Bottom) / dpi.DpiScaleY);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private const uint MonitorDefaultToNearest = 2;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out MonitorWorkAreas.NativeRect rect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
    }
}
