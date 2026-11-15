# System tray icon

Session 9 of `.scratch/review-hardening/plan.md` (wave 3, last). A notification-area icon with
saved-server quick-connect, Show and Exit, plus an opt-in "minimize to tray".

User decisions (fixed): the icon is always present while the app runs; opt-in Settings
checkbox makes minimize hide the window to the tray; closing the window always exits (no
close-to-tray).

## Implementation choice: `Shell_NotifyIcon` P/Invoke, native popup menu

- WinForms `NotifyIcon` would need `<UseWindowsForms>true</UseWindowsForms>`: the WinForms
  assemblies (System.Windows.Forms + System.Windows.Forms.Primitives + System.Drawing.Common
  et al.) then ride the self-contained single-file exe, and `ImplicitUsings` adds a global
  `System.Windows.Forms` using that makes `Application`/`Button`/`MessageBox`/... ambiguous
  project-wide. One icon and one menu do not justify either.
- A ~200-line `Views/TrayIcon` wrapper does the job with `Shell_NotifyIcon` (NOTIFYICON_VERSION_4)
  and `TrackPopupMenuEx`: zero new dependencies, zero csproj change. Published size is
  measured before/after (see the ticket report).
- The menu is the native Win32 popup (standard notification-area look, the shell's own
  dismissal rules: `SetForegroundWindow` on the owner before tracking, `WM_NULL` after).
  A WPF `ContextMenu` popup from the tray has well-known focus/dismissal quirks; not worth it.
- The icon lives on its OWN hidden tool window (a `HwndSource` with no `WS_VISIBLE`,
  `WS_EX_TOOLWINDOW`, a title distinct from `AppInfo.Title` so `FindWindow` never confuses
  it with the main window): `SetForegroundWindow` before the menu never pops the launcher
  window forward, and a hidden top-level window still receives the `TaskbarCreated`
  broadcast (the icon is re-added after an Explorer restart).
- Icon: the exe's own embedded icon (`ExtractIconEx(Environment.ProcessPath, 0)` small size —
  the same `Assets/launcher.ico` the `ApplicationIcon` embeds), falling back to the stock
  application icon. Tooltip: `AppInfo.Title`.
- The glue is untested (real shell, real HWNDs) and verified by the manual check. The menu
  model and the window decisions are tested.

## Menu model (`ViewModels/TrayMenu`, built by `MainViewModel.BuildTrayMenu()`)

Built fresh every time the menu opens, from `MainViewModel.SavedServers` (the full list in the
saved order; the search box filters only the view, never the tray), so adding, removing,
reordering and renaming are reflected by construction — no change tracking.

Entries, in order (`TrayMenuEntry(Kind, Label, Address, IsEnabled)`):
1. One `Server` entry per saved server, label = the server name, address = its address.
   - Cap: at most `TrayMenu.MaxServers` = 15 servers. Past the cap, a `ShowAll` entry
     ("All servers...") follows the 15th and opens the window. Long names are cut to 60
     characters with an ellipsis (a server name has no length limit; a menu wider than
     the screen is useless).
   - No saved servers: one disabled `NoServers` entry ("No saved servers").
   - While the VM is busy (a connect/open in flight) server entries are disabled. The click
     path ALSO goes through `HandleForwardedConnectAsync`, whose busy guard ignores it (no
     queueing): the menu can be open when a connect starts.
2. `Separator`.
3. `Show` (the default/bold item), `Exit`.

Labels come from the localizer at build time (5 new keys × 11 languages: `TrayShow`,
`TrayExit`, `TrayAllServers`, `TrayNoServers`, `MinimizeToTrayCheckbox`). The glue escapes
`&` (Win32 mnemonic prefix) when rendering; the model carries the plain name.

## Clicks

- Left click on the icon (`NIN_SELECT`/`NIN_KEYSELECT`) → show and activate the window.
  Picked over double-click: one click is what users try first, and a double-click is just two
  idempotent shows.
- `Show`, `ShowAll` → show and activate.
- `Server` → show and activate, then `HandleForwardedConnectAsync(address)`. Decision: a tray
  quick-connect behaves exactly like a desktop shortcut against the running instance (which
  also brings the window forward), so "Starting Steam...", "Invalid address" and other
  statuses are visible instead of silently lost behind a hidden window.
- `Exit` → `MainWindow.Close()`, i.e. the normal close path (`Closed` cancels the loops,
  app exits on last window close).

## Window decisions (`Views/TrayWindowController` over the `ITrayWindow` seam)

- `OnStateChanged()`: when `MinimizeToTray` is on and the window became Minimized while
  visible → `Hide()` (off the taskbar). Any non-minimized state is remembered as the restore
  state (Normal or Maximized).
- `ShowAndActivate()`: hidden → `Show()`; then, if minimized, `WindowState` = the remembered
  restore state (Normal by default); then `Activate()`. Show happens BEFORE the state change:
  the state is never changed on a hidden window.
- `IsAttended` = visible AND active AND not minimized. The composition root feeds
  `MainViewModel.SetWindowAttended(controller.IsAttended)` from `Activated`, `Deactivated`,
  `StateChanged` and `IsVisibleChanged` — a hidden window counts as away (the enrichment
  loop parks) and showing+activating resumes it.
- Turning the setting off while hidden is impossible (the checkbox is in the window); turning
  it off never un-hides anything.
- Hide/Show do not touch the view model or the visual tree: the update banner, the confirm
  overlay and the status line are exactly as they were when the window comes back.

## Bringing a hidden window back from another process

`Service/Win32ExistingWindowActivator` (duplicate launch, `--connect` shortcut,
`cfxlauncher://` link — all run it in the duplicate process) used to `ShowWindow(SW_RESTORE)`
the found window natively. On a WPF window hidden with `Hide()` that makes it visible behind
WPF's back (WPF still thinks it is hidden). New behaviour:
- `FindWindow` (finds hidden top-level windows too) → if `IsWindowVisible`: the old native
  restore + `SetForegroundWindow` (unchanged for the common case and for an older running
  copy).
- Hidden: `AllowSetForegroundWindow(<owner pid>)` + `PostMessage(handle, ShowWindowMessage)`,
  a registered message (`Service/ShowWindowMessage`, name `<ProductName>.ShowWindow`). The
  running instance hooks its main window and answers with `ShowAndActivate()` on the
  dispatcher — WPF does the show, so its state stays in sync; the foreground grant lets the
  `Activate()` actually take the foreground.
- The forwarded-connect handler (pipe) ALSO calls `ShowAndActivate()` before connecting
  (belt and braces: the message and the pipe race, and either alone restores the window).
- Found in the manual check: anything ELSE that natively shows the hidden window (an older
  copy's activator, a third-party tool, a posted `SC_RESTORE`/`SC_MINIMIZE` — a native
  minimize of a hidden window shows it minimized) left it natively visible while WPF still
  believed it hidden, so every later minimize stayed on the taskbar. `Views/MainWindowMessages`
  (the main-window hook, thin glue) watches `WM_SHOWWINDOW`/`SWP_SHOWWINDOW` and, deferred to
  the dispatcher, adopts a foreign show with a WPF `Show()` and re-runs the minimize decision.

## Icon lifetime (no ghost icon)

The icon is added once the main window and VM exist and removed (idempotent `Dispose`,
`NIM_DELETE`, icon handle destroyed) on: main window `Closed` (window close, tray Exit, and
`Application.Shutdown` — the auto-update relaunch exit action closes the windows), the
`Application.Exit` event, `AppDomain.ProcessExit`, and `AppDomain.UnhandledException` (a
crash that ends the process). A dispatcher exception handled by the last-resort handler keeps
the app — and its working icon — alive. Before the window exists there is no icon to leak.

## Settings

`LauncherSettings.MinimizeToTray` (default false), `MainViewModel.MinimizeToTray` persisted
on change like `AutoLaunch` (save failure → value reverts, `StatusSaveFailed`), Settings
checkbox `MinimizeToTrayCheckbox` below the web-links one.

## Out of scope

Close-to-tray (user decision), balloon notifications, dark-mode native menus, icon per
connection state.
