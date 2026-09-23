# 01: RefreshServersCommand on MainViewModel

**What to build:** `MainViewModel` exposes a `RefreshServersCommand` that the user can invoke
to force an immediate update of the saved servers' data: it runs the same forced-path refresh
introduced in phase 9 (`RefreshServerInfoAsync(forceRefresh: true)`), cannot overlap with
itself (disabled while running), leaves all rows untouched on outage (presence null → no-op),
and never writes to `StatusText` (the status line belongs to the connect flow).

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

`MainViewModel.RefreshServersCommand` (AsyncRelayCommand, `CanExecute = !IsRefreshingServers`)
runs `RefreshServerInfoAsync(forceRefresh: true)`, swallows refresh failures (rows untouched),
never touches `StatusText`; overlap prevented via `IsRefreshingServers` (covered by a
gated-fake test). Fake gained `ForcedRefreshCalls` + `RefreshDelay`. Suite 477 green.

- [ ] Executing the command triggers exactly one forced enrichment refresh
- [ ] Rows update from the refreshed presence (players/max, offline, icon)
- [ ] Outage (null presence) leaves rows as-is, no exception
- [ ] Command cannot overlap with itself
- [ ] `StatusText` is not modified by the command
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/saved-servers-refresh-button/spec.md`
