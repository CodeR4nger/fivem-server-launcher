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

**Status:** ready-for-agent

- [ ] The main view only composes the new controls; no area's markup remains inline in it.
- [ ] Shared styles/brushes/converters/storyboards live in one theme dictionary; no control redefines a shared key.
- [ ] Code-behind lives with the control it serves; no logic moves into XAML or out of the view models.
- [ ] Every XAML-instantiated type still has a true parameterless constructor (the reflection test pattern covers any new one).
- [ ] Before/after screenshots of every state (settings open, dev mode, browser, dialog, confirm, update banner, empty list) rendered from the real window match; recorded in this ticket.
- [ ] `SettingsPanel_ShouldShowEveryCheckboxUnclipped` and the whole suite pass unchanged; clean build has zero warnings.
- [ ] `AGENTS.md`'s UI bullet names the new controls and the theme dictionary.
