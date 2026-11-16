# 05: Open client split button with one preferred-client setting

**What to build:** the bottom bar's open-client control. Its main part reads "Open <preferred
client>" and opens it (same launch path and status messages as today's OPEN). Its ▾ part lists
installed clients with the current one marked; picking one saves it as the Preferred client and
launches nothing. The separate "selected for this session" client is gone: the button and the
Settings Preferred client control read and write the same state, so changing either updates
the other immediately. A preferred client that is no longer installed falls back to the first
installed one; with no client installed the button reads "No client installed" and is disabled.
Secondary styling (Connect stays the only amber primary on the Servers page); no CFX status dot
on it; no right-click behaviour.

See `../spec.md` (Open client button).

**Blocked by:** 04.

**Status:** done

- [x] One view-model state backs both editors; tests prove a change from either side is seen by the other and persisted once.
- [x] Save failure reverts the selection with the existing save-failed status (persistence convention).
- [x] Not-installed fallback and no-client disabled state tested.
- [x] Dropdown opens/closes from mouse and keyboard, Esc closes it, focus returns to the button.
- [x] Old OPEN split-button markup and its code-behind toggle are removed; tests that named the old selection are rewritten under new names.
- [x] Layout test for the bottom bar in all 11 languages (long client names do not clip); zero warnings.

## Comments

**Delivered (2026-11-15)** on `claude/ui-05-open-client-de84ab`.

### One preferred-client state

`MainViewModel.PreferredClientOption` is the only preferred-client state. The Settings combo
(still the old ComboBox until ticket 09) and the bottom bar's button both bind it, so a
change from either side is seen by the other and saved once through the existing setter; a
failed save reverts it and shows `StatusSaveFailed` (the `TrySaveSettings` convention).
`InitializeAsync` seeds it from `LauncherSettings.PreferredClient`, falls back to the first
installed client without rewriting the setting, and leaves it null when nothing is installed.

| Removed | Replaced by |
|---|---|
| `SelectedOpenClient` (session-only selection) | `PreferredClientOption` |
| `SelectedOpenClientLabel` (bare client name) | `OpenClientLabel` ("Open <client>" / "No client installed", re-emitted on language change) |
| `SelectOpenClientCommand` | `ChoosePreferredClientCommand` (saves, launches nothing) |
| — | `HasInstalledClients` (enables the ▾ part) |

`OpenClientCommand` is unchanged apart from opening `PreferredClientOption` (same launch
path and status messages).

### View

`Views/OpenClientButton`: two secondary pills (`OpenButton`, `ChooserButton` ▾). The old
in-layout `Canvas` dropdown and its height-animation toggle code-behind are gone. The client
list is now a `ContextMenu` (`ClientMenu`, theme `PopupMenuStyle` / `PopupMenuItemStyle`,
`Placement=Top`, 6 px above the button row and at least as wide) with a ✓ on the preferred
client. A ContextMenu brings the keyboard model (arrows, Enter picks, Esc closes); the thin
code-behind opens it from click / Enter / Space / Alt+Down, focuses the current client on
open, gives focus back to ▾ after a keyboard close, and drops the ▾ click that is the same
press that dismissed the menu (400 ms guard, the saved-row kebab's pattern). No CFX dot, no
right-click. `MenuRowButtonStyle` stays in the theme (pinned by `ThemeStyleTests`) but has no
consumer now.

New keys ×11: `OpenClientButton`, `OpenClientNone`, `OpenClientChooser` (▾ accessible name
and tooltip). Spanish curated; Japanese and Polish "no client" strings were shortened to fit.

### Tests

- `MainViewModelOpenClientTests.cs`: both editors, persisted once, failed-save revert,
  fallback, no-client state, seeding, picking launches nothing, language re-emit. The old
  tests that named the session selection were deleted and rewritten under new names.
- `MainViewModelOpenClientLayoutTests.cs`: the real window at 1000×640 in all 11 languages
  with the longest label ("Open FiveM Enhanced") and with "No client installed" — nothing in
  the bottom bar clipped and the button clear of the nav tabs; the menu lists the installed
  clients and marks only the preferred one; picking through the item's invoke path saves and
  closes; with no client both parts are disabled and the menu stays closed.
- Full suite green on three runs; `--no-incremental` build has zero warnings.

### Screenshots (`../screenshots/05-open-client/`)

- `01-button.png`: the real app (Debug build, fake data), captured with PrintWindow.
- `02-dropdown-open.png`, `03-no-client-installed.png`, `04-maximized-size.png`,
  `05-maximized-size-dropdown-open.png`, `06-german-longest-label.png`: the real `MainWindow`
  rendered offscreen by the test harness with fake install locators (motion off). The open
  menu is the real `ClientMenu` style and items rendered standalone and drawn at its
  placement, because the popup itself is a separate window. The large shots are the window
  at 1920×1040, not a native maximize. The status line reads "Listo" there: a harness
  artefact (the VM is built in the system language before the test language is applied).

### Not verified live

The user's desktop was in use (a game in the foreground) during this work, so the real app
was not focused, maximized or given keyboard input, and its menu was not opened (a
ContextMenu captures the mouse). Live keyboard behaviour (Enter/Space/Alt+Down to open,
arrows, Enter to pick, Esc returning focus to ▾) and a native maximize screenshot are still
to be checked by hand.
