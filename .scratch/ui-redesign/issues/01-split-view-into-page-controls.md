# 01: Split the view into page controls and a theme dictionary

**What to build:** a refactor with no visible change. The single main view is split into one
control per area — shell (settings bar, CFX status, open-client button, footer area), saved
servers area, server browser, settings panel, Dev Mode panel, server dialog, confirm dialog,
update banner — and every shared brush, style, template, converter instance and storyboard
moves into one theme resource dictionary loaded once for the app. Each piece of code-behind
moves with the control it serves (drag-to-reorder, kebab/context-menu glue, dropdown toggle).
The app looks and behaves exactly as before; this only makes the following tickets safe.

See `../spec.md` (Implementation Decisions → Structure).

**Blocked by:** None (can start immediately).

**Status:** done

- [x] The main view only composes the new controls; no area's markup remains inline in it.
- [x] Shared styles/brushes/converters/storyboards live in one theme dictionary; no control redefines a shared key.
- [x] Code-behind lives with the control it serves; no logic moves into XAML or out of the view models.
- [x] Every XAML-instantiated type still has a true parameterless constructor (the reflection test pattern covers any new one).
- [x] Before/after screenshots of every state (settings open, dev mode, browser, dialog, confirm, update banner, empty list) rendered from the real window match; recorded in this ticket.
- [x] `SettingsPanel_ShouldShowEveryCheckboxUnclipped` and the whole suite pass unchanged; clean build has zero warnings.
- [x] `AGENTS.md`'s UI bullet names the new controls and the theme dictionary.

## Comments

**Delivered (2026-11-15).** `Views/MainView.xaml` now only composes ten UserControls:
`SettingsBar`, `SettingsPanel`, `CfxStatusPanel`, `OpenClientButton`, `DevModePanel` (left
frame), `SavedServersPanel`, and the root overlays in their old z-order `UpdateBanner` →
`ServerDialog` (loopback side panel included) → `ServerBrowser` → `ConfirmDialog`. Shared
resources moved verbatim into `Views/Theme.xaml`. Code-behind moved with its control: the
dropdown glue went to `OpenClientButton`, reorder/drag/kebab/menu glue to `SavedServersPanel`,
and the full-width slide to `ServerBrowser`. The browser's slide distance is still the full
content width, because the control spans the whole root grid. `MainView.xaml.cs` is just
`InitializeComponent`.

**Theme loading.** The theme is not in `App.xaml`, for two reasons. A nested UserControl's BAML
resolves `StaticResource` at its own parse time, before it has a parent. And
`SettingsPanel_ShouldShowEveryCheckboxUnclipped` builds `MainWindow` with no `Application`.
Each control instead sets `<views:ThemeResources/>` as its Resources: a thin dictionary that
merges one `Theme` instance, cached per UI thread. That keeps it parsed once in the app, and
per thread because styles are thread-bound and the tests use their own STA threads. Implicit
styles stay control-scoped as they were (view-scoped before), so no app-wide implicit-style
semantics changed. New tests: `ThemeResourcesTests` covers the parameterless constructors of
`Theme` and `ThemeResources`, the shared instance per thread, and resolution of the shared keys.

**Bindings.** None needed changing. Every control inherits the MainViewModel DataContext, so
every `RelativeSource AncestorType=UserControl` binding now finds the nearest page control and
reads the same view model. That covers the row `Tag` behind the context-menu
`PlacementTarget.Tag` bindings, the move arrows, the OPEN dropdown items and the browser row
buttons. Each `ElementName` (`ServerSearchBox`, `DropIndicatorCanvas`, `BrowserSearchBox`,
`CloseHold`) stays inside one control.

**Screenshot comparison.** A throwaway probe (deleted, never committed) rendered the real
`MainWindow` offscreen to PNG with an English view model and three saved servers. It waited
for the fade/slide animations to finish, then captured 17 states, first on `HEAD` (a scratch
worktree) and then on this branch:
empty list, main, settings open, Dev Mode, browser, add dialog, add dialog with
`localhost:30120` (loopback side panel), edit dialog, confirm delete, update available, update
failed, search filter active, OPEN dropdown open, kebab menu open, row right-click menu open,
after a drag-equivalent `MoveServerToIndex`, and browser opened then closed. A pixel-by-pixel
RGB comparison found **0 differing pixels in all 17 states**. A sanity pair of two different
states reported 17,478 differing pixels, so the comparator does detect changes.

**Binding errors.** The same probe attached a listener to
`PresentationTraceSources.DataBindingSource` (Warning level). Its capture was proven with a
deliberately broken canary binding, which was logged. Across all 17 states it logged **zero
`System.Windows.Data` errors**, on both `HEAD` and the branch. In both the kebab menu and the
right-click menu, all three items (Edit, Create shortcut, Delete) resolved a non-null command
and the row as their parameter, identically before and after.

**Runtime smoke.** The built `CFXLauncher.exe` ran with scratch `launcher-settings.json`
(`AutoLaunch` false) and `saved-servers.json` beside it, with no other instance running. It
started, stayed responsive with its window titled, and exited 0 on close. The scratch files
were deleted afterwards.

**Not verified by hand.** No desktop-control tool was available, so real mouse interaction
in the running app was not exercised. That covers the kebab's 400 ms toggle guard, a real
drag with the drop line, hover arrows and the combo popup animations. Those code paths moved
verbatim and their bindings and renders are covered above, but a manual click-through is
still advisable.

Suite: 1195/1195 green (the full suite ran green five times in total) (`SettingsPanel_ShouldShowEveryCheckboxUnclipped`
unchanged). `dotnet build FiveMServerLauncher.slnx --no-incremental`: 0 warnings, 0 errors.
