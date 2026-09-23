# 01: Search filtering on MainViewModel

**What to build:** `MainViewModel` exposes `ServerSearchText`; the visible saved-servers list
is filtered live (as you type) by case-insensitive substring on the server name OR address.
Empty/blank text shows everything. Filtering never mutates the underlying collection or the
repository; enrichment (presence/icons) keeps applying to all rows whether visible or not; a
row filtered out of view is simply hidden. The search text is session-only (never persisted).

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Typing a name fragment filters the visible rows (case-insensitive)
- [ ] Typing an address fragment filters the visible rows
- [ ] Non-matching rows are hidden, not removed
- [ ] Clearing the box restores the full list
- [ ] Enrichment keeps applying to hidden rows
- [ ] Search text is not persisted anywhere
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/saved-servers-search/spec.md`
