# 08: Save triggers immediate data refresh

**What to build:** after the add/edit dialog saves a server, that server's data (presence +
icon) refreshes immediately via the forced path (ticket 07) instead of waiting for the
per-minute cycle. The save flow awaits the refresh before showing "Server saved" so the row
lands populated on first paint; refresh failure/outage degrades gracefully (row stays plain,
save still succeeds). The per-minute loop cadence is untouched.

**Blocked by:** 07 (Forced-refresh seam + shared cooldown).

**Status:** resolved

## Answer

`SaveServerDialogCommand` is now an `AsyncRelayCommand`; after persisting (add and edit
paths) it awaits pending CfxId captures then `RefreshServerInfoAsync(forceRefresh: true)`
before showing "Server saved"; a throwing refresh is swallowed so the save never fails. Suite
473 green.

- [ ] Saving (add path) triggers exactly one forced enrichment refresh
- [ ] Saving (edit path) triggers exactly one forced enrichment refresh
- [ ] The saved row shows fresh presence/icon immediately after save when available
- [ ] Refresh failure during save does not fail the save itself
- [ ] Per-minute loop unchanged
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9d)
