# 10: Cleanup and final pass

**What to build:** the redesign finished and documented. Remove styles, converters,
code-behind, view-model members and localization keys that nothing renders any more (the
localization audit and the build confirm). Update `AGENTS.md` (UI bullets describe the shell,
pages, theme, motion switch and layout tests; stale descriptions of the old overlays, kebab and
banner removed) and the README (new screenshots if it has any, the Inter license note). Do a
full manual pass and record it here: minimum size and maximized; English, Spanish, Russian and
Japanese; keyboard-only navigation of every page and dialog; animations on, Minimal, and with
Windows animations off; tray minimize/restore; a `cfxlauncher://` link and a duplicate launch
restoring the window; the published single-file exe (Inter renders, size before/after noted).

See `../spec.md`.

**Blocked by:** 06, 07, 08, 09.

**Status:** done

- [x] No orphaned styles, converters, members or keys remain (list what was removed).
- [x] `AGENTS.md` and README match the delivered UI.
- [x] Manual pass results recorded with screenshots; any issue found is fixed or filed as a new ticket.
- [x] Published exe checked; suite green on several full runs; zero warnings. CI: not run from here (the branch is not pushed); `ci.yml` runs the same `dotnet test`.

## Comments

**Delivered (2026-11-16)** on `claude/ui-10-cleanup-fac72c`. Screenshots in
`../screenshots/10-final-pass/`.

### 1. Removed (each confirmed unused first)

- **Localization keys, from all 11 files (11):** `SettingsButton`, `BackButton`,
  `SavedServersHeader`, `BrowseButton`, `NewServerButton`, `ConnectAddressHeader`,
  `EnterServerButton`, `BrowseServersHeader`, `BrowserLoadFailed`, and `StatusOffline` /
  `StatusUnresolved` (orphaned by the `StatusLabel` removal below; the new audit flagged them).
- **Converter:** `InverseBooleanConverter` (`InverseBoolean`) and its tests.
- **Theme keys:** `SlideSurfaceStyle`, `MenuRowButtonStyle`, `ListRowStyle`, `ListWellStyle`,
  the whole unused spacing ramp `SpaceXs`…`SpaceXxl`, and `MotionSlow`. Their
  `ThemeStyleTests` theory rows went with them.
- **View-model members:** `SavedServerItem.TrimmedName` and the row's `IsSelected` (it only fed
  `TrimmedName`; the VM's fan-out loop went too), the test-only `SavedServerItem.HasIcon`, and
  the old `SavedServerItem.StatusLabel` (OFFLINE / UNRESOLVED). The Servers page renders
  `ListStatusText`; tests that used `StatusLabel` as their enrichment probe now assert
  `ListStatusText`.
- **Kept, because later tickets gave them consumers:** `InverseBoolToVisibility` (Servers
  requirements "None", Settings "No client installed") and `SelectionEqualityToVisibility`
  (the open-client menu's ✓).
- **Audit test:** `Localization/LocalizationKeyUsageTests` scans `src/FiveMServerLauncher` at
  test time (repo root found by `FiveMServerLauncher.slnx`) and fails when an `en.json` key has
  no `{loc:Loc Key}` in XAML and no `"Key"` literal in code. Keys built at run time go on its
  commented `DynamicallyBuiltKeys` allowlist, which is empty today; a second test keeps that
  list from naming deleted keys. Seen RED against the pre-cleanup `en.json` (listed the nine
  keys) and again when `StatusLabel` went.
- **Other scans:** every control-local `x:Key` and every converter class has a consumer. The
  remaining public members used by no XAML or other source file are command bodies, the attached-property
  accessors XAML needs, and test seams (`FiveMStatus`/`RedMStatus`/…, `DetailStateLabel`).
  All are used, so they stay.

### 2. `TextInputStyle` padding

The template bound `Padding` as the content host's margin, and the TextBox also hands
`Padding` to its text view, so every input drew twice its inset. The host margin is gone.
Text now starts at BorderThickness + Padding + the text view's own 2 px gutter, and a
placeholder's left margin is that same sum. Pinned by:
- `TextInput_ShouldInsetItsTextByThePaddingOnce` (both input styles; the gutter is measured
  from the same style with no padding or border, so there is no magic number; RED was 27
  against 15);
- `PlaceholderInput_ShouldStartTypedTextAtThePlaceholdersLeftEdge` (Servers search, address,
  Browse search);
- `DialogField_ShouldInsetItsTextLikeTheDialogsComboBoxes`.

Workarounds removed: Servers `Padding` 7,4 / 8,6 → 14,8 / 16,12 (the inset they actually drew;
the "2 × Padding + 3" comment is gone); dialog `FieldInputStyle` 5,0 → 9,0; Browse 18,0,6,0 →
34,0,12,0.

Before/after, offscreen renders diffed pixel by pixel:
- Servers inputs: pixel-identical.
- Browse search: typed text moved 2 px left. It had sat right of its placeholder (39 against
  37); it now starts on the placeholder's edge (`padding-browse-search-before-after.png`).
- Dialog fields: text moved 1 px left onto the combo boxes' 12 px content inset (it was 13)
  (`padding-dialog-fields-before-after.png`).
- Every other render: identical.

### 3. Offline dot

`ServerDetailStateToBrushConverter` draws Offline in grey `#7A7A7A`, the same as Not listed.
Listed stays green and Manual stays amber. Its theory row was RED first.

### 4. Docs

- `AGENTS.md`: the UI bullets describe the delivered shell, pages, theme (padding rule,
  status-dot colours), motion switch, dialogs and layout tests. Removed: overlays, kebab,
  gutter arrows, banner, settings panel and checkboxes, removed members and styles, the "theme
  pitfall". Added: the key audit, the input-inset tests, the accessibility rule and the
  forwarded-connect page switch.
- `README.md`: the feature list and usage describe the pages, Connect, Open client and the
  switches. It has no screenshots, so none were added; the Inter / OFL note stays.
- `CHANGELOG.md`: an `[Unreleased]` entry for the redesign. `AppInfo.Version` is unchanged.

### 5. Final pass

**Offscreen renders** at 1000×640 and 1920×1040 in en / es / ru / ja: Servers, Browse,
Settings General and Dev mode, the server dialog (loopback section open) and the confirm
dialog. Also standalone, per language: the ⋮ menu and the open-client menu (all clients
installed). Files: `<surface>-<lang>-<size>.png`, `servers-menu-<lang>.png`,
`open-client-menu-<lang>.png`. No clipping or overlap was found. The bodies scroll as
designed at the minimum size (the dialog with loopback open, the Settings General tab).

**Real app** (published single-file exe, a scratch folder with fake TEST-NET servers and
`AutoLaunch` false; the desktop had no game running and had been idle for ~5 min; driven
through UI Automation + SendKeys, captures by PrintWindow; Enter was never sent to a row, the
address field, Connect, Open or Launch):
- **Keyboard:** checked on every page and in both dialogs. Tab walks page → bottom bar → title
  bar and wraps.
  - Enter on a tab switches page; Left/Right switches the Settings tabs.
  - The server dialog's Tab cycle stays inside the card, including the loopback fields (Game
    build is skipped while Game is None, as designed), and Esc returns focus to "+".
  - The ⋮ menu opens on Enter and arrows move through it (Move up skipped on the first row).
    Delete opens the confirm focused on CANCEL; Esc returns focus to ⋮. Nothing was saved or
    deleted.
- **Motion:** compared one frame 40 ms after a page switch with the settled frame.
  - Default: 6,124–6,778 sample pixels differ (the page is mid-fade,
    `runtime-motion-default-40ms-after-switch.png`).
  - Minimal: 0 differ; it applied at once and persisted.
  - Windows "Show animations" off: **not checked live**. Turning it off changes a system
    setting, which this session does not do. The seam is covered by the
    `SystemAnimationSetting`/`IsMotionEnabled` tests.
- **Tray:** with Minimize to tray on, minimize hides the window (not visible, process alive).
  A duplicate launch exits and restores it to the foreground. The tray icon's own click and
  menu were not exercised: no mouse input on the shell.
- **Web link:** with web links on, the key was written with the exe's command line, and
  `cfxlauncher://connect/not..a..valid..address` restored the hidden window. The address was
  invalid on purpose, so the connect stopped at "Invalid address" and nothing launched.
  Disabling removed the key; `HKCU\Software\Classes\cfxlauncher` did not exist before and does
  not exist after.
- **Persistence:** language (system = Spanish here), Minimal, tray and maximized all came back
  after a restart. The window was restored maximized on its monitor
  (`runtime-after-restart-maximized-es.png`).
- **Published exe:** built and published with the `AGENTS.md` commands.
  - `main` (b2100e9): 66,681,009 B. This branch: 66,679,361 B (−1,648 B).
  - It starts, renders Inter and shows each page (`runtime-published-exe-start.png`); the
    final build was re-started to confirm the fixes below.

**Found and fixed** (each RED first):
- The Preferred-client segments announced `FiveMServerLauncher.ViewModels.InstalledClientOption`
  to screen readers, and the game, language, build, sort and game-filter options announced a
  record dump. Each option type's `ToString()` is now its label (`OptionAccessibleNameTests`).
- The All / Online / Offline chips and the ⋮ menu had no accessible name.
- Tab reached ↻ before "+" and Browse's Sort before the game chips (right-docked controls
  declared first). Both rows now declare their controls in reading order and stay
  pixel-identical (`HeaderAndFilterRows_ShouldTabLeftToRight`).
- A web link, shortcut or tray connect arriving on Browse or Settings reported its outcome on
  the hidden Servers page's status line. It now shows Servers first, like a browser-row
  connect (`HandleForwardedConnectAsync_FromAnotherPage_ShouldShowTheServersPage`).

**Recorded, not changed:**
- The status line keeps the language it was emitted in until the next status. A live language
  switch leaves "Ready" in the old language. This is by design ("VM strings resolve at
  emission"); startup is correct because the saved language applies before the first status.
  The offscreen renders show "Listo" because the test helper switches language after building
  the view model.
- `ServerMark` initials take the first letters of the first two words, so "Server 29" reads
  "S2". This is ticket 06's rule and works as designed.

### Tests

- The executed total went 1635 → 1641, stable across three full runs. The deliberate changes:
  - Removed (−13): `InverseBooleanConverterTests` (4); the four `ThemeStyleTests` rows for
    the deleted styles (4); the four `TrimmedName` / `IsSelected` notification tests (4); and
    `StatusLabel_WhenLanguageSwitched_ShouldRefresh` (1).
  - Renamed by rewrite (the old names claimed "Unresolved"):
    `Ctor_WithoutCfxId_ShouldShowNotListedWithNoGame` and
    `SetCfxId_ShouldFlipStatusFromNotListedToOffline`.
  - Added (+19): the key audit (2), the theme inset theory (2), the placeholder theory (3), the
    dialog-field inset (1), the forwarded-connect page tests (3), the option names (6), the
    chip/menu names (1) and the tab order (1).
- `dotnet build FiveMServerLauncher.slnx --no-incremental`: 0 warnings.

