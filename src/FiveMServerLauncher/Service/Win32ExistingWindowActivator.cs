using System.Runtime.InteropServices;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

public sealed class Win32ExistingWindowActivator : IExistingWindowActivator
{
    public void ActivateExisting()
    {
        var handle = FindWindow(null, AppInfo.Title);

        if (handle == IntPtr.Zero)
        {
            return;
        }

        ShowWindow(handle, SwRestore);
        SetForegroundWindow(handle);
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
}
