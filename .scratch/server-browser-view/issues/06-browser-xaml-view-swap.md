# 06: Browser XAML + view swap

**What to build:** the full content-area browser view (not a new window): opened from a
"BROWSE SERVERS" button on the main view and closed via a back affordance; while open, other
overlays (settings, dev mode, server dialog) are closed. Rows are fixed-height and virtualized
(icon, name, game tag, players/max, Connect/Save actions); icons load lazily for visible rows
only via `GetIconAsync`, never bulk-downloaded. Composition root wires the
`ServerBrowserViewModel` sharing the existing `ServerCatalog` and enrichment instances.

**Blocked by:** 02 (ServerBrowserViewModel with filters).

**Status:** resolved

## Answer

Browser overlay in `MainView.xaml` (full content area, visible when `IsServerBrowserOpen`):
header BACK + title + ↻ refresh; filters row (game ComboBox via `GameFilterOption` list,
hide full/empty checkboxes, name search with placeholder); virtualized fixed-height ListBox
(recycling); lazy per-row icons (`Icon` getter triggers a one-shot loader →
`GetIconAsync`); SAVED marker; CONNECT/SAVE row actions bound to the MainViewModel commands;
honest empty-state on outage. Composition root shares the single ServerCatalog/enrichment/
repository. Suite 509 green.

- [ ] BROWSE SERVERS opens the browser view over the whole content area; back closes it
- [ ] Opening closes settings/dev/dialog overlays
- [ ] List is virtualized with fixed-height rows (33k rows stay responsive)
- [ ] Icons load only for visible rows
- [ ] Composition root shares the single ServerCatalog/enrichment instances
- [ ] Build green; visual verification by the user

**Spec:** `.scratch/server-browser-view/spec.md`
