# 02: Refresh button in the saved-servers header

**What to build:** a small icon-only refresh button (↻) sits in the saved-servers panel
header row, to the right of the SAVED SERVERS title, bound to `RefreshServersCommand`. While
a refresh is in flight (or cooling down) the button is visibly unavailable/inert per the
existing busy-flag pattern. XAML only binds; no code-behind.

**Blocked by:** 01 (RefreshServersCommand on MainViewModel).

**Status:** resolved

## Answer

Icon-only ↻ button in the SAVED SERVERS header (right-aligned via DockPanel), transparent
template with secondary-grey → amber hover, dimmed when `CanExecute` is false. Bug found in
manual testing (button re-enabled on scroll before cooldown) fixed by holding the disabled
state through the cooldown window in the command itself. Suite 478 green.

- [ ] Button visible at the top of the saved-servers panel, right-aligned in the header row
- [ ] Executes `RefreshServersCommand`
- [ ] Disabled/inert state while a refresh is running
- [ ] Styling consistent with existing secondary/icon buttons
- [ ] Build green; visual verification by the user

**Spec:** `.scratch/saved-servers-refresh-button/spec.md`
