# 03: Migrate all UI strings to the localizer

**What to build:** Every visible string routes through the localizer: XAML labels via a thin
localization markup extension (settings/dev buttons, section headers, dialog fields/buttons,
browser chrome, tooltips); view-model strings (status messages, dialog titles and errors, row
tags, status labels, filter options) resolve at emission time and re-emit on the language-changed
event. The English UI must read exactly as before the migration.

**Blocked by:** 01 (dictionary key set), 02 (live switch plumbing).

**Status:** ready-for-agent

- [ ] XAML static labels render from keys through the markup extension and refresh on language
      change
- [ ] Interpolated status messages localize with their parameters (app names, client names)
- [ ] Dialog titles, validation errors, row tags, CFX status labels, browser filters and
      placeholders all localize
- [ ] Switching language live updates every visible surface without restart
