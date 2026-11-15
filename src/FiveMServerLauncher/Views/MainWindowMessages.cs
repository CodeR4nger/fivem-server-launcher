using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using FiveMServerLauncher.Service;

namespace FiveMServerLauncher.Views;

/// <summary>
/// Thin, untested main-window hook that keeps a tray-hidden window in WPF's hands:
/// <list type="bullet">
/// <item>A duplicate process (second launch, shortcut, web link) posts
/// <see cref="ShowWindowMessage"/> instead of natively showing the hidden window; the
/// answer is the WPF show-and-activate.</item>
/// <item>Anything else that natively shows the hidden window (an older copy's activator,
/// a third-party tool) leaves it visible while WPF still believes it hidden — the next
/// minimize would then never hide it again. A native show of a WPF-hidden window is
/// resynced with a WPF Show.</item>
/// </list>
/// </summary>
public static class MainWindowMessages
{
    private const int WmShowWindow = 0x0018;
    private const int WmWindowPosChanged = 0x0047;
    private const int SwpShowWindow = 0x0040;

    // WINDOWPOS: hwnd, hwndInsertAfter, x, y, cx, cy, flags.
    private static readonly int WindowPosFlagsOffset = (2 * IntPtr.Size) + (4 * sizeof(int));

    public static void Attach(Window window, TrayWindowController trayWindow)
    {
        var handle = new WindowInteropHelper(window).Handle;

        HwndSource.FromHwnd(handle)?.AddHook((IntPtr _, int message, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            if (message == ShowWindowMessage.Id)
            {
                trayWindow.ShowAndActivate();
                handled = true;
            }
            else if (IsShowing(message, wParam, lParam))
            {
                // Deferred: never re-enter WPF's show/hide from inside the message. WPF's
                // own Show is visible to WPF by then, so only a foreign show gets through;
                // it is adopted, then the minimize decision runs again (a foreign
                // SC_MINIMIZE shows a hidden window minimized, after StateChanged ran).
                window.Dispatcher.BeginInvoke(() =>
                {
                    if (!window.IsVisible && IsWindowVisible(handle))
                    {
                        window.Show();
                        trayWindow.OnStateChanged();
                    }
                });
            }

            return IntPtr.Zero;
        });
    }

    private static bool IsShowing(int message, IntPtr wParam, IntPtr lParam) =>
        (message == WmShowWindow && wParam != IntPtr.Zero)
        || (message == WmWindowPosChanged
            && (Marshal.ReadInt32(lParam, WindowPosFlagsOffset) & SwpShowWindow) != 0);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);
}
