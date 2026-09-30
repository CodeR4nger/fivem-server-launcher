# 01: Loading state for the browser's first fetch

**What to build:** Opening the browser while the catalog is still downloading shows a
centered, localized "Loading servers..." line instead of a silent empty list. The line shows
only while the initial fetch is in flight and the list is empty — a re-fetch over existing
rows never flashes it, and it never appears alongside the failure text. When the download
lands the line disappears with the rows; on outage it clears and leaves the existing
failure state.

**Blocked by:** None (can start immediately).

**Status:** resolved (build + suite 742 green; visually verified by the user. VM state TDD'd with a gated fake HTTP handler)

- [ ] `LoadAsync` raises the refreshing flag around the fetch, owning it only when not
      already refreshing (the manual-refresh cooldown window keeps the flag up, untouched)
- [ ] The in-flight flag is observable with a gated fake HTTP handler: overlay true while
      the first fetch runs, false once it lands
- [ ] A re-fetch over existing rows never shows the overlay
- [ ] An outage clears the flag and the overlay, leaving the failure state alone
- [ ] The overlay notifies on refreshing/rows/load-failed changes (computed property)
- [ ] One new localized key (`BrowserLoading`) in every shipped language file, audit green
- [ ] Build green, full suite green
- [ ] Manual visual verification: open the browser on a cold cache, see the loading line,
      see it replaced by rows

**Spec:** `.scratch/browser-load-feedback/spec.md`
