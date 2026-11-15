# tray-glue-and-activation

Status: done

See ../spec.md "Implementation choice", "Bringing a hidden window back", "Icon lifetime". `Views/TrayIcon`
(Shell_NotifyIcon + TrackPopupMenuEx, untested glue), `Service/ShowWindowMessage`, activator hidden-window path,
`Views/MainWindowMessages` (show-message answer + foreign-show resync), App.xaml.cs wiring (attendance, forwarded
connect shows the window, disposal on every exit path).

## Published size (documented build + publish commands, win-x64 single-file, compressed)
- before (main @ 687d632): 65,620,171 B
- after: 65,626,696 B (+6,525 B). No csproj change.
- For reference, WinForms would have added System.Windows.Forms (13.7 MB) + .Primitives (3.6 MB) +
  .Design (6.1 MB) + System.Drawing.Common (0.9 MB) uncompressed, absent from today's WPF-only output.

## Manual check (2026-11-15, Debug build copied to a scratch folder with seeded data; published exe smoke-tested)
Driven through Win32 (posted tray callbacks, menu read via MN_GETHMENU, keys posted to the menu
queue) and UI Automation; every saved server was a CFX id verified to 404, so no connect could launch FiveM.
- Icon present (NIM_MODIFY probe) from startup; menu = saved servers in order (`Tray && Bravo` escaped),
  separator, Show (default), Exit; ESC dismisses.
- Minimize with the setting on hides (off taskbar); left click restores Normal + foreground; a window
  maximized before minimizing comes back maximized via Show.
- Settings checkbox toggled through UIA persists both ways; off → minimize stays on the taskbar.
- Hidden + duplicate launch → restored, duplicate exited. Hidden + `--connect x1` → restored, address x1,
  "Invalid address". Hidden + `CFXLauncher.exe cfxlauncher://connect/x2` → restored, address x2, "Invalid address".
- Hidden + tray quick-connect "Tray Alpha" → window restored to foreground, "Invalid address".
- Open Settings panel and status line intact after hide/show.
- Found and fixed: a foreign native SC_RESTORE/SC_MINIMIZE on the hidden window desynced WPF (later minimizes
  stayed on the taskbar); 4/4 rounds stable after `MainWindowMessages` resync.
- Exit from the menu (from hidden) and WM_CLOSE (window close path) both end the process and remove the icon.
- Published single-file exe: icon (embedded, extractable), menu, hide/restore, close all work.

## Not verified
- `start "cfxlauncher://..."` through the shell: enabling web links (registry write) was denied by the
  session's auto-mode permission check, so the link was passed to the exe directly (same command line the
  registration runs; shell routing itself was verified in session 8). `HKCU\Software\Classes\cfxlauncher`
  was absent before and after.
- Real mouse right-click/left-click on the icon (simulated with the shell's callback messages), the menu's
  look, Explorer-restart re-add, crash-path removal (`AppDomain.UnhandledException`) and the update relaunch
  exit (rides `Shutdown` → window `Closed`, the same path as the verified close).
