using System.Runtime.InteropServices;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

public sealed class Win32ExistingWindowActivator : IExistingWindowActivator
{
    public void ActivateExisting()
    {
        // FindWindow also finds hidden top-level windows (a window minimized to the tray).
        var handle = FindWindow(null, AppInfo.Title);

        if (handle == IntPtr.Zero)
        {
            return;
        }

        if (IsWindowVisible(handle))
        {
            ShowWindow(handle, SwRestore);
            SetForegroundWindow(handle);
            return;
        }

        // Hidden in the tray: ask the owner to show itself through WPF, and lend it this
        // (foreground) process's right to take the foreground so its Activate() works.
        if (GetWindowThreadProcessId(handle, out var ownerProcessId) != 0)
        {
            AllowSetForegroundWindow(ownerProcessId);
        }

        PostMessage(handle, ShowWindowMessage.Id, IntPtr.Zero, IntPtr.Zero);
    }

    private const int SwRestore = 9;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
}
