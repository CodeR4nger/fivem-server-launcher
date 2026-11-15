using System.Runtime.InteropServices;
using System.Windows;

namespace FiveMServerLauncher.Views;

/// <summary>
/// Thin, untested P/Invoke glue behind <see cref="IMonitorWorkAreas"/>: every monitor's
/// work area (EnumDisplayMonitors + GetMonitorInfo), converted from physical pixels to
/// WPF DIPs with the system DPI (the process is system-DPI-aware, so WPF window
/// coordinates are physical pixels divided by that scale). Any failure answers no
/// monitors, which makes the rules centre the window.
/// </summary>
public sealed class MonitorWorkAreas : IMonitorWorkAreas
{
    public IReadOnlyList<Rect> GetWorkAreas()
    {
        var areas = new List<Rect>();

        try
        {
            var scale = GetDpiForSystem() / 96.0;

            if (scale <= 0)
            {
                scale = 1;
            }

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr _, IntPtr _, IntPtr _) =>
            {
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };

                if (GetMonitorInfo(monitor, ref info))
                {
                    var work = info.Work;
                    areas.Add(new Rect(
                        work.Left / scale,
                        work.Top / scale,
                        (work.Right - work.Left) / scale,
                        (work.Bottom - work.Top) / scale));
                }

                return true;
            }, IntPtr.Zero);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            return [];
        }

        return areas;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public int Flags;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();
}
