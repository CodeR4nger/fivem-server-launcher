# 07: Server and confirm dialogs as modals

**What to build:** the add/edit server dialog and the confirm dialog in the mockup's modal
style: dimmed backdrop, centred card with title and close ✕, Name and Address fields, Requires
Steam / Requires Discord switches, inline validation error, Cancel and amber Save. The loopback
local-connection parameters (cfx id, game, build, pure mode) become a section inside the
dialog shown only for loopback addresses, replacing the side panel. Confirmations (delete,
shortcut overwrite) use the same modal with Cancel and a confirm button. Esc cancels, Enter
submits, focus moves into the dialog on open, stays trapped while open, and returns to where it
was on close. Backdrop fades and the card pops in. View-model contracts are unchanged.

See `../spec.md` (Dialogs).

**Blocked by:** 04.

**Status:** done

- [x] Every existing dialog/confirm view-model test passes unchanged.
- [x] Loopback section appears and disappears with the address; its values save as today.
- [x] Esc/Enter/focus behaviour verified manually and recorded (thin glue, no logic in code-behind).
- [x] The old side panel and old overlay markup are removed.
- [x] Layout test for the dialog (with and without the loopback section) in all 11 languages; zero warnings.

## Comments

**Delivered (2026-11-15)** on `claude/ui-07-dialogs-bc7140`.

### What changed

- **Add/edit dialog** (`Views/ServerDialog`):
  - a `ScrimBrush` scrim with one centred `ModalFrameStyle` card (`ServerDialogCard`, 520 wide), as in the mockup;
  - the header has the title and a ✕ (new key `DialogClose`, accessible name only);
  - the body is a `ScrollViewer` (`ServerDialogBody`) holding Name and Address (44 px inputs) and the Requires Steam / Requires Discord rows, which are `ToggleSwitchStyle` switches in one grouped block;
  - the loopback-only `LoopbackSection` sits in the body: cfx id, game, build and pure mode in a 2×2 grid;
  - the footer stays in view: the `DialogError` line (collapsed while empty), then Cancel (secondary) and Save (amber primary).
- **Loopback section:** it replaces the side panel that slid in from the right, and is visible while `IsDialogLocalhost` is true. When it appears it is scrolled into view (one line of code-behind glue), because at 1000×640 it starts below the body's fold. Its values bind to the same properties as before, so saving is unchanged.
- **Confirm dialog** (`Views/ConfirmDialog`): the same scrim and card (`ConfirmDialogCard`, 440 wide) with the existing text, Cancel and Confirm.
  - Confirm is drawn as a danger pill (`DangerTextBrush` fill, `OnSelectedBrush` text, 7.9:1, pinned in `ThemeTokenTests`) when the new `MainViewModel.IsConfirmDestructive` is set. That comes from the new `RequestDestructiveConfirmation(text, action)`, which delete uses.
  - `RequestConfirmation(text, action)` is unchanged and stays non-destructive (shortcut overwrite).
  - The style is local to the control (`ConfirmButtonStyle`); `Theme.xaml` is untouched.
- **Motion:** the scrim fades (`FadeSurfaceStyle`) and the card pops in (`ModalFrameStyle` scale; it fades with the scrim). Both are keyed on `Motion.Shown` = the open flag, so they snap when motion is off.
- **Removed:** the side panel, the horizontal `StackPanel` wrapper and the old checkbox/caption markup. `SlideSurfaceStyle` is now unused by any XAML, so ticket 10 can drop it.

### Keyboard and focus (thin glue)

- **Keys:** `KeyBinding`s on each card.
  - **Esc** runs the cancel command.
  - **Enter** runs Save or Confirm, with these exceptions:
    - A focused button handles its own Enter first, so Enter on Cancel cancels.
    - The switches set `KeyboardNavigation.AcceptsReturn=False`, so Enter on them saves (Space toggles).
    - An open ComboBox consumes its own Enter and Esc.
- **`Views/ModalFocus.IsOpen`** (attached, bound to the open flag on the card):
  - **On open:** records the opener via `ReturnTargetFor`, which is unit-tested (4 cases): the focused element, or the `PlacementTarget` of the ContextMenu/Popup it sits in, because that popup is gone by close time. After layout (Input priority) it moves focus to the card's first tab stop: Name in the server dialog, Cancel in the confirm (the safe default for a delete).
  - **While open:** `PreviewLostKeyboardFocus` refuses focus moves to other elements of the same window, such as a click on the title bar. Popups (combo lists) are other HWNDs and pass. The Tab and Ctrl+Tab cycle is `KeyboardNavigation.TabNavigation=Cycle` on the card; the body `ScrollViewer` is not a tab stop.
  - **On close:** focuses the opener, or its nearest focusable visual ancestor (a context menu's placement target is the row Grid, so focus lands on the ListBoxItem). It only does this when focus is still the card's or was dropped.
  - **Header order:** the header is declared last in the card's Grid, so the tab order is Name → Address → Steam → Discord → Cancel → Save → ✕. The first real-app pass found focus opening on the ✕, where Enter closed the dialog.

### Tests

- `Dialog_AtMinimumSize_ShouldFitTheOverlayAndClipNothing`: 11 languages × three dialogs (add; loopback section open with a validation error; confirm delete with a long server name). It checks that:
  - the card is shown and lies inside the overlay (content panel + bottom bar), and the overlay inside the window;
  - every visible element fits its layout slot, and no non-wrapping text is wider than its box;
  - every element lies inside the card, except scrolled content, which is measured against its own extent.
- **Mutation check:** with the body's vertical scrolling disabled, the loopback case fails with "StackPanel is 462×440 in a 462×345 slot". At the minimum size the open loopback section needs about 95 px of scrolling.
- **VM tests:** `DeleteServerCommand_ShouldAskForADestructiveConfirmation`, `RequestConfirmation_AfterADestructiveOne_ShouldNotBeDestructive` and `CreateDesktopShortcutCommand_WhenShortcutExists_ShouldAskForANonDestructiveConfirmation`. Every existing dialog and confirm test passes unchanged.
- **Helper change:** `ShowShell` takes an optional repository.

### Screenshots (`../screenshots/07-dialogs/`)

Rendered offscreen from the real window. "maximized" is a 1920×1032 window, the size of a maximized window on a 1080p work area.

- At 1000×640: `add-min.png`, `edit-min.png`, `loopback-min.png` (body scrolled to the section), `validation-error-min.png`, `confirm-delete-min.png` (danger pill), `confirm-shortcut-overwrite-min.png` (amber).
- Maximized: `add-maximized.png`, `loopback-maximized.png` (fits without scrolling), `confirm-delete-maximized.png`.

### Real-app check

Debug exe, no other launcher running, scratch `saved-servers.json` with fake rows and `launcher-settings.json` with `AutoLaunch` false and `Language` "en"; both were deleted afterwards. The app was driven by UI Automation and SendKeys, with a guard that aborts unless the launcher is the foreground window.

- **"+ NEW SERVER" → Enter:** the dialog opens with focus on Name.
  - 16 Tabs cycle Name → Address → Steam → Discord → Cancel → Save → ✕ → Name…
  - Shift+Tab goes backwards and wraps the same way.
- **Address field:** typing `localhost:30120` shows the section; replacing it with `203.0.113.5:30120` hides it.
- **Esc:** closes the dialog, and focus is back on "+ NEW SERVER".
- **Enter:**
  - on an empty form it keeps the dialog open with the inline error;
  - with the Game combo open (Alt+Down), Enter selects and closes the list while the dialog stays open;
  - the next Esc closes the dialog, and focus returns to "+ NEW SERVER".
- **Row right-click → Edit server:** the dialog opens with focus on Name; Esc returns focus to that row's list item.
- **Row right-click → Delete server:** the confirm opens on Cancel, Tab cycles Confirm ↔ Cancel, and Esc closes it with the row kept and focus back on the row.

### Found in passing (not fixed here, shared theme)

`TextInputStyle` applies `Padding` twice: once as the content host's margin, and again because the TextBox passes its `Padding` to that host. A box with `Padding` 8 draws a 16 px inset. The dialog compensates locally (`FieldInputStyle` `Padding` 5). Fixing the theme template is left to ticket 10, since other pages are being restyled in parallel and may be compensating too.

