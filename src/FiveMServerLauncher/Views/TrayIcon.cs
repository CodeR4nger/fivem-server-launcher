using System.Runtime.InteropServices;
using System.Windows.Interop;
using FiveMServerLauncher.ViewModels;

namespace FiveMServerLauncher.Views;

/// <summary>
/// The notification-area icon: thin, untested Shell_NotifyIcon glue (see
/// .scratch/tray-icon/spec.md). It owns a hidden tool window of its own — so opening the
/// menu never brings the launcher window forward and the TaskbarCreated broadcast (an
/// Explorer restart) still arrives — renders <see cref="TrayMenuEntry"/> lists as a native
/// popup menu, and forwards clicks. Every decision lives in TrayMenu and
/// TrayWindowController.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private const int WmApp = 0x8000;
    private const int CallbackMessage = WmApp + 1;
    private const uint IconId = 1;

    private readonly HwndSource _source;

    // Cached: RemoveIcon may run off the UI thread or after the source is destroyed.
    private readonly IntPtr _handle;
    private readonly string _tooltip;
    private readonly Func<IReadOnlyList<TrayMenuEntry>> _buildMenu;
    private readonly Action<TrayMenuEntry> _onSelected;
    private readonly Action _onActivate;
    private readonly uint _taskbarCreated;
    private readonly IntPtr _icon;
    private readonly bool _ownsIcon;
    private int _removed;

    public TrayIcon(
        string tooltip,
        Func<IReadOnlyList<TrayMenuEntry>> buildMenu,
        Action<TrayMenuEntry> onSelected,
        Action onActivate)
    {
        _tooltip = tooltip;
        _buildMenu = buildMenu;
        _onSelected = onSelected;
        _onActivate = onActivate;
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        (_icon, _ownsIcon) = LoadAppIcon();

        // No WS_VISIBLE: never shown. The title differs from AppInfo.Title so the
        // duplicate-instance FindWindow can never pick this window.
        _source = new HwndSource(new HwndSourceParameters($"{tooltip} (tray)")
        {
            WindowStyle = 0,
            ExtendedWindowStyle = WsExToolWindow,
            Width = 0,
            Height = 0
        });
        _source.AddHook(WndProc);
        _handle = _source.Handle;

        AddIcon();
    }

    /// <summary>
    /// Removes the icon from the notification area. Thread-agnostic and idempotent, so the
    /// process-exit and unhandled-exception hooks can call it from any thread.
    /// </summary>
    public void RemoveIcon()
    {
        if (Interlocked.Exchange(ref _removed, 1) == 1)
        {
            return;
        }

        var data = NewData();
        Shell_NotifyIcon(NimDelete, ref data);

        if (_ownsIcon)
        {
            DestroyIcon(_icon);
        }
    }

    public void Dispose()
    {
        RemoveIcon();

        if (_source.CheckAccess())
        {
            _source.RemoveHook(WndProc);
            _source.Dispose();
        }
    }

    private void AddIcon()
    {
        if (Volatile.Read(ref _removed) == 1)
        {
            return;
        }

        var data = NewData();
        data.uFlags = NifMessage | NifIcon | NifTip | NifShowTip;
        data.uCallbackMessage = CallbackMessage;
        data.hIcon = _icon;
        data.szTip = _tooltip.Length < 128 ? _tooltip : _tooltip[..127];
        Shell_NotifyIcon(NimAdd, ref data);

        var version = NewData();
        version.uVersion = NotifyIconVersion4;
        Shell_NotifyIcon(NimSetVersion, ref version);
    }

    private NotifyIconData NewData() => new()
    {
        cbSize = Marshal.SizeOf<NotifyIconData>(),
        hWnd = _handle,
        uID = IconId,
        szTip = string.Empty,
        szInfo = string.Empty,
        szInfoTitle = string.Empty
    };

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == CallbackMessage)
        {
            // NOTIFYICON_VERSION_4: the event in LOWORD(lParam), the anchor point in wParam.
            switch ((int)(lParam.ToInt64() & 0xFFFF))
            {
                case WmContextMenu:
                    var anchor = wParam.ToInt64();
                    ShowMenu((short)(anchor & 0xFFFF), (short)((anchor >> 16) & 0xFFFF));
                    break;
                case NinSelect:
                case NinKeySelect:
                    _onActivate();
                    break;
            }

            handled = true;
        }
        else if (message == _taskbarCreated)
        {
            // Explorer restarted and forgot every icon.
            AddIcon();
        }

        return IntPtr.Zero;
    }

    private void ShowMenu(int x, int y)
    {
        var entries = _buildMenu();
        var menu = CreatePopupMenu();

        try
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var id = (UIntPtr)(uint)(i + 1);

                if (entry.Kind == TrayMenuEntryKind.Separator)
                {
                    AppendMenu(menu, MfSeparator, UIntPtr.Zero, null);
                    continue;
                }

                AppendMenu(menu, MfString | (entry.IsEnabled ? 0 : MfGrayed), id, ToMenuText(entry.Label));

                if (entry.Kind == TrayMenuEntryKind.Show)
                {
                    SetMenuDefaultItem(menu, (uint)(i + 1), 0);
                }
            }

            // The documented notification-area dance: own the foreground while tracking so
            // a click elsewhere dismisses the menu, then nudge the window afterwards.
            SetForegroundWindow(_handle);
            var chosen = TrackPopupMenuEx(
                menu,
                TpmReturnCmd | TpmRightButton | TpmNoNotify | TpmWorkArea,
                x,
                y,
                _handle,
                IntPtr.Zero);
            PostMessage(_handle, WmNull, IntPtr.Zero, IntPtr.Zero);

            if (chosen > 0 && chosen <= entries.Count)
            {
                var selected = entries[chosen - 1];

                // Leave the window procedure before acting (Exit closes windows, connect awaits).
                _source.Dispatcher.BeginInvoke(() => _onSelected(selected));
            }
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    // '&' is the Win32 mnemonic prefix and a tab starts the accelerator column.
    private static string ToMenuText(string label) => label.Replace("&", "&&").Replace('\t', ' ');

    private static (IntPtr Icon, bool Owned) LoadAppIcon()
    {
        // The exe's own embedded icon (ApplicationIcon = Assets/launcher.ico), small size.
        var small = new IntPtr[1];

        if (Environment.ProcessPath is { } path
            && ExtractIconEx(path, 0, null, small, 1) > 0
            && small[0] != IntPtr.Zero)
        {
            return (small[0], true);
        }

        return (LoadIcon(IntPtr.Zero, (IntPtr)IdiApplication), false);
    }

    private const int WsExToolWindow = 0x00000080;
    private const uint NimAdd = 0x0;
    private const uint NimDelete = 0x2;
    private const uint NimSetVersion = 0x4;
    private const uint NifMessage = 0x1;
    private const uint NifIcon = 0x2;
    private const uint NifTip = 0x4;
    private const uint NifShowTip = 0x80;
    private const uint NotifyIconVersion4 = 4;
    private const int WmNull = 0x0000;
    private const int WmContextMenu = 0x007B;
    private const int NinSelect = 0x0400;
    private const int NinKeySelect = 0x0401;
    private const uint MfString = 0x0;
    private const uint MfGrayed = 0x1;
    private const uint MfSeparator = 0x800;
    private const uint TpmRightButton = 0x2;
    private const uint TpmNoNotify = 0x80;
    private const uint TpmReturnCmd = 0x100;
    private const uint TpmWorkArea = 0x10000;
    private const int IdiApplication = 32512;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "ExtractIconExW")]
    private static extern uint ExtractIconEx(string file, int index, IntPtr[]? large, IntPtr[]? small, uint count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadIconW")]
    private static extern IntPtr LoadIcon(IntPtr instance, IntPtr name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegisterWindowMessageW")]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "AppendMenuW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string? text);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetMenuDefaultItem(IntPtr menu, uint item, uint byPosition);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr parameters);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr menu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
}
