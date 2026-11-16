# 09: Settings page

**What to build:** the Settings page from the mockup, scrolling, with General and Dev mode
tabs. General: Launch (Preferred client as a segmented control of installed clients — the same
state as the open-client button — and Auto-launch), Integration (web links, minimize to tray),
Display (language, Interface animations Default / Minimal), About (launcher version, Check for
updates). Each row has a title, a one-line description and a switch or control; switches are the
theme's toggle switch. Check for updates is a new command running the existing release-feed
check on demand (busy while running; result "Up to date", the title-bar pill, or "Could not
check for updates"; only when the update feature is wired). Dev mode tab: Client (FiveM /
RedM), Game build and Pure mode (existing persisted values and option engine), Launch and
Launch second client. Tab content slides in.

See `../spec.md` (Settings stories, Check for updates, Interface animations).

**Blocked by:** 04, 05.

**Status:** done

- [x] Every existing setting keeps its persistence and failure-revert behaviour (existing tests pass).
- [x] Interface animations UI drives the setting from ticket 03; Minimal turns motion off immediately.
- [x] Check for updates tested with the release-feed fake: up to date, update found, failure, busy gate, feature unwired.
- [x] Old settings panel and Dev Mode panel markup removed; `SettingsPanel_ShouldShowEveryCheckboxUnclipped` is deleted and replaced by a Settings page layout test (both tabs, minimum size, all 11 languages).
- [x] New strings in all 11 languages; zero warnings.

## Comments

### Delivered

- **Page** (`Views/SettingsPage`): a fixed header (the "Settings" title, then the General / Dev mode tabs as a segmented ListBox on the right) above one scroller. Header and content share a column capped at 900 px, so a maximized window keeps rows readable. Each tab fades and rises in on a switch (`PageSurfaceStyle`) and snaps when motion is off. `SettingsPanel` and `DevModePanel` are deleted.
- **General:** four sections, Launch, Integration, Display and About, each a section header plus a `SettingsGroupStyle` card of `SettingsRowStyle` rows (title, description, control), split by hairlines.
  - Preferred client is a segmented control over `AvailableOpenClients`, bound to `PreferredClientOption`, so it and the bottom-bar button are one state. With no client installed it shows "No client installed" instead.
  - Auto-launch, web links and minimize-to-tray are theme toggle switches. Web links keep the registry-first apply and failure revert.
  - Language is the theme dropdown, sized to its widest item (200–320 px).
  - Interface animations is a Default / Minimal segmented control on `InterfaceAnimations`. Minimal turns `IsMotionEnabled` off at once (pinned by a window test).
  - About shows `AppInfo.Version`, and the Updates row holds Check for updates with its result line beside it.
- **Dev mode:** the explanatory line, then one "Client overrides" group: Client (FiveM / RedM segments), Game build and Pure mode, followed by Launch (primary) and Launch second client (secondary), both on `DevLaunchCommand`.

### View-model changes

- New: `SettingsTab` (enum General | DevMode; session-only, starts on General, survives page switches), `IsGeneralSettingsTab`, `IsDevModeSettingsTab`.
- New: `CheckForUpdatesCommand` / `CheckForUpdatesAsync`, `IsCheckingForUpdates`, `UpdateCheckText`.
- `DevClient` now has a public setter that accepts only FiveM and RedM. It replaces `ToggleDevClientCommand`, `DevClientButtonText` and `DevClientLabel`, which are removed. Their three tests were deleted; tests that toggled in their Given step now set `DevClient`.

### Check for updates decisions

- **Busy** means `IsCheckingForUpdates`, not the global `IsBusy`, so a check never blocks connecting. The row shows "Checking for updates..." meanwhile.
- **Gate:** the command can't run while the startup or a manual check is in flight (the startup check now sets the same flag), or while an update is applying.
- **Feed returns null:** "You're up to date". The feed's contract folds an outage into null, so an offline check also reads up to date. Telling the two apart needs a feed contract change; that is a follow-up.
- **Feed throws:** "Could not check for updates".
- **Update found:** the title-bar pill, as at startup. It clears a failed pill so the states stay exclusive, and the row shows no line.
- **Unwired feature** (null seams): the button shows disabled and nothing runs (pinned).
- The startup check stays silent and never writes the row line.

### Tests

- 14 view-model tests: tab, check-for-updates states and gates, language re-emit, `DevClient`.
- `SettingsPage_AtMinimumSize_ShouldClipNothing`: 11 languages × 2 tabs. Every client is installed and the longest About line shows. The tab scrolls, so only the window-bottom check is waived; its viewport width is still checked, and descriptions wrap.
- Five window binding tests: no-client note, preferred-client sync with the open-client button, Minimal turns motion off, tab pick, dev-client pick.
- `SettingsPage_ShouldShowEveryCheckboxUnclipped` and its helpers were deleted.
- The full suite passes on 3 runs at a stable executed total of 1635 (main's 1608 − 14 deleted + 41 new).
- `dotnet build FiveMServerLauncher.slnx --no-incremental`: 0 warnings.
- 31 new keys in all 11 languages, Spanish curated. The orphaned `DevModeButton`, `PreferredOpenClientHeader`, `AutoLaunchCheckbox`, `WebLinksCheckbox`, `MinimizeToTrayCheckbox`, `LaunchButton`, `LaunchSecondClientButton`, `LanguageHeader` and `DevClientButton` were removed.

### Screenshots (`../screenshots/09-settings-page/`)

Offscreen renders of the real window at 1000×640, captured after the animations settle:

- `general-top`, `general-about`;
- `update-checking`, `update-up-to-date`, `update-failed`, `update-found` (the pill is in the title bar);
- `dev-mode`, `no-client-installed`;
- `general-de`, `general-ru`, `dev-mode-ru`;
- `maximized` (1920×1040).

### Real-app check — not done

Another application (a game client) held the foreground, so the launcher was not started, to avoid stealing focus.

- **Checked instead:**
  - The live GitHub `releases/latest` answers `v1.3.1`, equal to `AppInfo.Version`, so the real feed returns null and the row would say "You're up to date".
  - Persistence and failed-save reverts are covered by the existing view-model tests, which still pass.
  - Minimal turning motion off and the preferred-client sync are covered by window tests.
- **Still to check by hand:**
  - toggles persisting across a real restart;
  - the animations visibly stopping on Minimal;
  - clicking the segments and the bottom-bar menu in the running app;
  - Check for updates against GitHub through the UI.

### Not verified

- A non-100 % DPI.
- Screen-reader announcements of the segmented controls.
