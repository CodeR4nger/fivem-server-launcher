# 04: Shell and navigation

**What to build:** the new frame around the pages. The window is resizable with a 1000×640
minimum and default, maximizable, and restores its saved size, position and maximized state
(off-screen positions fall back to centred). The custom title bar gains maximize/restore and
double-click maximize, and hosts the update notice as a pill (available / in progress / failed,
Update and dismiss, same commands and texts as the old banner). One rounded content panel
shows the current page; the bottom bar holds the page tabs (Servers, Browse, Settings) with a
sliding highlight, CFX status at the left, and a placeholder slot for the open-client button
(ticket 05). The "CFX Launcher by CodeRanger" footer is always visible, above every overlay.
The existing content is re-homed unchanged inside the pages: saved servers and the connect
field on Servers, the browser on Browse (no longer an overlay), the settings panel and Dev Mode
panel on Settings. Pages fade/slide in.

See `../spec.md` (Window, Pages, Update notice, Interface animations).

**Blocked by:** 03.

**Status:** done

- [x] `CurrentPage` replaces browser-open / settings-open / dev-mode as navigation state; the browser view model survives page switches.
- [x] Window size/position/maximized persist and restore; minimum enforced; maximize works from button and double-click; tray restore still returns to normal or maximized.
- [x] Update pill covers all three states and dismiss rules exactly as the banner did (existing update tests still pass, rewritten only where a name became false).
- [x] Footer stays visible above dialogs and confirm.
- [x] Nav highlight slides; pages animate in; both off when motion is off.
- [x] Every control reachable by Tab in a sensible order; nav tabs work from the keyboard.
- [x] A shell layout test (minimum size, all 11 languages) passes; screenshots at minimum size and maximized recorded; zero warnings.

## Comments

**Delivered (2026-11-15)** on `claude/ui-04-shell-navigation-78ee09`.

### Navigation state

`MainViewModel.CurrentPage` (`ViewModels/AppPage`: Servers, Browse, Settings; starts on
Servers) is now the whole navigation state. `IsServersPage` / `IsBrowsePage` /
`IsSettingsPage` are derived from it and re-raised on every change. `NavigateCommand`
(parameter: the page) and `NavigateAsync(page)` are the only way to change it.

| Removed | Replaced by |
|---|---|
| `IsServerBrowserOpen` | `CurrentPage == Browse` (`IsBrowsePage`) |
| `IsSettingsOpen` | `CurrentPage == Settings` (`IsSettingsPage`) |
| `IsDevMode` | gone: Dev Mode is part of the Settings page (`IsSettingsPage`); ticket 09 adds `SettingsTab` |
| `SettingsCommand`, `ToggleDevModeCommand` | `NavigateCommand` with `AppPage.Settings` |
| `OpenServerBrowserCommand` | `NavigateCommand` with `AppPage.Browse` (the saved list's BROWSE button uses it) |
| `CloseServerBrowserCommand` and the browser's BACK button | the nav tabs; connecting from the browser returns to Servers |
| the SETTINGS / DEV MODE bar (`Views/SettingsBar`) | the bottom-bar page tabs |
| the bottom update banner (`Views/UpdateBanner`) | the title-bar pill (`Views/UpdatePill`), same flags, commands, texts and dismiss rules |
| the browser's full-width slide (code-behind) | the page fade/rise (`PageSurfaceStyle`) |

Tests that named the old state were deleted and rewritten under new names:
`ToggleDevModeCommand_*`, `SettingsCommand_*` → `NavigateCommand_ShouldShowOnlyTheRequestedPage`
and the other navigation tests; `ConnectFromBrowserAsync_ShouldConnectByCfxIdCloseBrowserAndPersist`
→ `…ReturnToServersAndPersist`; `SettingsPanel_ShouldShowEveryCheckboxUnclipped` →
`SettingsPage_ShouldShowEveryCheckboxUnclipped`, which navigates to the Settings page first.
The update tests were left unchanged and still pass.

**Browser survives switches.** Browse calls `ServerBrowserViewModel.EnsureLoadedAsync()`. The
catalog loads on the first visit, and again only after a visit that left no rows (an outage).
It never loads while a load is already running. The view model and its row collection are
never rebuilt, so filters, sort, search, rows and scroll position survive. Pinned by
`NavigateAsync_BackToBrowse_ShouldKeepFiltersSortSearchAndRows` (same instances, one catalog
request) and `…AfterAFailedLoad_ShouldTryAgain`.

### Shell

- **Content panel:** one rounded panel (`ContentPanelStyle`) holds the three pages, each using
  `PageSurfaceStyle` with `Motion.Shown` bound to its page flag. A shown page fades and rises
  12 px over 250 ms when motion is on; a hidden page collapses at once. The page content is
  re-homed unchanged:
  - **Servers:** the saved-servers panel, kept at its old width (560 max) and centred.
  - **Browse:** the browser, without its overlay background, slide or BACK button.
  - **Settings:** the settings group and the Dev Mode group side by side, under a page title.
- **Bottom bar** (`Views/BottomBar`):
  - **Left:** CFX status, compacted to three dot + name + status rows (header moved to the
    automation name).
  - **Centre:** the three tabs in a fixed-width row (128 each, `NavTabWidth`) on a pill track.
    One highlight moves by its `TranslateTransform`: it slides with motion on and snaps with
    motion off.
  - **Right:** the existing OPEN split button, unchanged except that its client list now
    unrolls **upward** over the panel. It sits in a zero-height `Canvas`, which never clips its
    child and adds nothing to the bar's layout.
- **Overlays:** the add/edit dialog and the confirm dialog cover the panel and the bar.
- **Footer and title bar:** both live in `MainWindow`, outside the overlays' row, so they stay
  visible above every dialog (see `dialog-over-browse.png`, `confirm-over-servers.png`).
- **Keyboard:** checked live through UI Automation.
  - Tab order is title bar (pill when shown, minimize, maximize, close) → page → bottom bar
    (status is not focusable, then the three tabs, then OPEN and ▾), and it wraps.
  - Left/Right move between the tabs and cycle round; Enter and Space switch the page.
  - The tabs use the theme's `PillFocusVisual`, and so do the caption buttons now.
- **New localization keys** in all 11 files (Spanish curated): `NavServers`, `NavBrowse`,
  `NavSettings`, `WindowMinimize`, `WindowMaximize`, `WindowRestore`, `WindowClose`,
  `DismissUpdate`.
- **Left for ticket 10's cleanup:** this change orphaned the keys `SettingsButton` and
  `BackButton`. The `InverseBoolean` and `InverseBoolToVisibility` converters are now unused
  by any XAML.

### Window and the WPF pitfalls

- **Size:** 1000×640 is both the default and the minimum (`WindowPlacementRules` constants,
  bound by `MainWindow`). `ResizeMode=CanResize`.
- **Edge resizing** needs both `ResizeMode=CanResize` and a non-zero
  `WindowChrome.ResizeBorderThickness`; it is kept at 6. Verified live by dragging the right,
  bottom and left edges. Dragging smaller than 1000×640 stops at 1000×640.
- **Hit-testing in the chrome:** the old title bar was a `DragMove` border marked hit-testable,
  which swallowed Windows' own double-click maximize. It is gone. Everything in the 40 px
  caption that is not marked `IsHitTestVisibleInChrome` is now the native caption, so drag,
  double-click maximize/restore, Aero snap and the system menu come from Windows. The caption
  buttons (style) and the update pill (on its root, inherited by its buttons) are marked
  hit-testable. Verified live: a double-click on the title bar maximizes, a second one
  restores, and the buttons and pill still take clicks.
- **Maximize button:** its glyph (☐ / ❐) and accessible name (Maximize / Restore) follow
  `WindowState` through a style DataTrigger.
- **Maximized overhang / taskbar:** a maximized WindowChrome window keeps its invisible frame.
  Measured live: window rect (−8,−8)–(1928,1040) on a (0,0)–(1920,1032) work area. Without a
  fix the content edges are cut and the bottom 8 px sit under the taskbar. `MainWindow` insets
  `RootGrid` by exactly the overhang. It measures it natively (`GetWindowRect` against the
  window monitor's `GetMonitorInfo` work area, divided by the window DPI) on state, size,
  location and DPI changes. `runtime-maximized.png` is the work area only: the footer ends
  exactly at the taskbar edge, and the taskbar stays visible above it.
- **Persistence:**
  - `LauncherSettings.Window` (`WindowPlacement` record) holds the normal-state bounds and
    `IsMaximized`.
  - `MainViewModel.WindowPlacement` goes through `SetPersisted<T>`: a failed save reverts and
    shows the save-failed status, and was added to the shared revert theory. An equal value
    writes nothing.
  - It is saved on `Closing` and on every `StateChanged`.
- **Restore rules:** `WindowPlacementRules`, tested over the `IMonitorWorkAreas` seam. The real
  `MonitorWorkAreas` is `EnumDisplayMonitors` + `GetMonitorInfo` work areas divided by the
  system DPI.
  - **Size:** clamped to the minimum; a non-finite value falls back to the default.
  - **Position:** kept only while at least 100×20 DIPs of the title strip sit on one current
    work area. Otherwise the window centres (`CenterScreen`) and keeps its size and maximized
    state.
  - **Order:** the normal bounds are applied first, then the maximized state, so maximize lands
    on the stored monitor and restore returns to the stored rectangle.
- **Tray:** the placement is applied before the `TrayWindowController` is built, so a maximized
  start is its restore state. `TrayWindowController.RestoreState` (newly exposed, tested)
  decides `IsMaximized` for a window closed while minimized.

**Real-app check** (Debug exe, no other instance running). The scratch
`launcher-settings.json` had `AutoLaunch` false. The window was driven by real mouse input and
`SetWindowPos`/`WM_CLOSE` from a script.

- **Resize and minimum:** right edge +202 px, bottom +102 px, left +102 px. Shrinking past the
  minimum stopped at 1000×640.
- **Maximize and restore:** a title-bar double-click and the button both maximize and restore,
  back to the exact previous rectangle.
- **Taskbar:** a maximized window does not cover it.
- **Saved values:** after moving to 200,150 at 1200×760, maximizing and closing, the file held
  `{"Left":200,"Top":150,"Width":1200,"Height":760,"IsMaximized":true}`.
- **Restart:** the window opened maximized. Minimize-to-tray hid it; a duplicate launch, which
  goes through the tray show path, restored it **maximized**; restoring then gave 200,150
  1200×760.
- **Off-screen fallback:** a stored position of −5000,3000 at 1100×700 opened centred at
  410,166, still 1100×700.
- **Scratch data:** deleted afterwards. The app's legacy migration had copied the real
  `%LOCALAPPDATA%` saved-servers file into the bin folder on the first run. Only that copy was
  deleted; the original is untouched. The runtime screenshots were retaken with a fake list
  and cropped to the window or work area.

### Tests

- **New:**
  - navigation (7);
  - window placement at the view-model seam (3) and in the rules (13 tests, 20 cases);
  - `FileSettingsStorage` round trip and missing field;
  - the tray restore state;
  - `PageSurfaceStyle` motion on/off/re-show/switch-off (4);
  - nav highlight snap (3 pages) and slide;
  - the shell layout test `Shell_AtMinimumSize_ShouldShowThePageAndClipNothingInItsBars`:
    11 languages × 3 pages, the update pill showing. It checks the window is 1000×640, only
    the current page is visible and fills the panel, and every visible element in the title
    bar, bottom bar and footer fits its layout slot and its region, with every text within
    its natural width.
- **Checked against mutations:**
  - The first version of the layout test compared `DesiredSize`, which WPF clamps to the
    offered space, and passed with 50 px tabs. The final version fails on them ("text
    'Servidores' is 68×16 in a 26×16 slot").
  - Removing the page storyboard's exit removal fails the motion-off test.
  - Forcing the highlight to snap fails the highlight tests.
- **Results:** the suite passes 1424/1424 and was green on 4 consecutive full runs.
  `dotnet build FiveMServerLauncher.slnx --no-incremental`: 0 warnings, 0 errors.

### Screenshots (`../screenshots/04-shell-and-navigation/`)

- **Offscreen renders of the real window** (throwaway probe, deleted):
  - `min-servers`, `min-browse` and `min-settings` at 1000×640;
  - `large-*` at 1920×1040 (the maximized client size);
  - `pill-available`, `pill-in-progress` and `pill-failed`;
  - `dialog-over-browse` and `confirm-over-servers`;
  - `open-client-list` (it unrolls upward).
- **Live captures:** `runtime-maximized` and `runtime-restored`.

### Not verified

- A second monitor and a non-100 % DPI: only one 1920×1080 monitor at 96 DPI was available.
  The DIP conversion and the monitor seam are written for both, but the off-screen rule was
  only exercised live on one monitor.
- The hover/press feel of the new tabs by a person, and a live Windows "Show animations" off
  run. The highlight and page snap paths are covered by tests.

