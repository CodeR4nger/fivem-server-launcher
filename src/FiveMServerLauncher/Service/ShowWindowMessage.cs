using System.Runtime.InteropServices;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

// A registered window message both ends derive here: a duplicate process (second launch,
// --connect shortcut, cfxlauncher:// link) posts it to a main window hidden in the tray, and
// the running instance answers by showing the window through WPF — a native ShowWindow on a
// WPF window hidden with Hide() would make it visible behind WPF's back.
public static class ShowWindowMessage
{
    public const string Name = $"{AppInfo.ProductName}.ShowWindow";

    private static readonly Lazy<uint> RegisteredId = new(() => RegisterWindowMessage(Name));

    public static uint Id => RegisteredId.Value;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string message);
}
