# 05: Settings UI panel

**What to build:** the settings surface is visible: a gear (⚙) button on the main window toggles an
inline panel bound to the settings state. Inside: the preferred-client selector (a bound list of
installed clients) and the auto-launch checkbox; both update the view model and save immediately.
The panel hides/shows via the gear command; dismissed state persists only in the window session
(no storage flag — repurpose on phase 5 if needed).

**Blocked by:** 02 (VM reads settings: preferred-client seeding + save-on-change).

**Status:** resolved

- [x] Gear/`⚙ SETTINGS` button (bound to `SettingsCommand`) toggles `IsSettingsOpen`; panel
      visibility binds through a `BooleanToVisibilityConverter` resource.
- [x] Preferred-client ComboBox lists installed clients (`AvailableOpenClients`), selected item
      two-way bound to `PreferredClientOption` (persists on change); auto-launch CheckBox binds to
      `AutoLaunch`.
