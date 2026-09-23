# v1.1 Phase 10 — Refresh button on the saved-servers list

Status: ready-for-agent

## Problem Statement

Saved-server rows refresh on a fixed per-minute cycle, and the underlying catalog is
TTL-cached, so a user who knows a server's data just changed (restarted, player count moved)
has no way to say "update now". Waiting up to a minute — and sometimes still getting the
cached snapshot — feels broken.

## Solution

A refresh button at the top of the saved-servers list that forces a fresh update of the
listed servers' data (presence + icons), bypassing the TTL cache, with a cooldown so the
expensive streamRedir download can't be spammed.

## User Stories

1. As a user, I want a refresh button on the saved-servers panel, so that I can update my
   servers' data on demand.
2. As a user, I want the refresh to actually fetch fresh data, not serve the cached snapshot,
   so that the button is meaningful.
3. As a user, I want the button to be visibly unavailable/inert while a refresh is running or
   cooling down, so that I understand why nothing happens on a second click.
4. As a user, I want a failed refresh (outage) to leave my last known data untouched, so that
   a flaky network doesn't blank my list.
5. As a user, I want the list itself to keep scrolling and the rest of the UI to stay
   responsive during a refresh, so that the app never freezes.

## Implementation Decisions

- Button placement: header row of the saved-servers panel (next to the "SAVED SERVERS"
  title). Icon-only (↻ glyph) consistent with the existing minimal UI; no text.
- Command on `MainViewModel`: `RefreshServersCommand` → runs the same forced-path refresh
  used by save-triggers-refresh (phase 9's `RefreshAsync(force: true)` + presence/icon
  application on rows with a `CfxId`).
- Cooldown and TTL-bypass semantics are owned by the enrichment service seam from phase 9
  (~15 s, injectable); the button reuses it rather than adding a second timer.
- Disabled/busy state: the command is disabled while a refresh is in flight (AsyncRelayCommand
  `IsExecuting` pattern already used by `ConnectCommand`) and shows a subtle visual state
  during cooldown. During cooldown refusal the command still completes successfully — it just
  serves the cached snapshot (the user already saw fresh data seconds ago).
- `StatusText` is NOT touched by a manual refresh (the status line belongs to the connect
  flow); the rows updating is the feedback.
- No changes to the per-minute loop; manual refresh does not reset the loop's schedule.
- Business logic stays in the ViewModel; XAML wires the button and its visibility/enabled
  states only.

## Testing Decisions

- ViewModel tests with `FakeServerEnrichmentService` (+ fake time) only: command invokes a
  *forced* refresh; command cannot overlap with itself; rows update from the refreshed
  presence; outage (null presence) leaves rows as-is.
- Cooldown semantics themselves are tested at the enrichment-service seam in phase 9 (request
  counts via `RoutedHttpMessageHandler`); here we only assert the VM delegates to the forced
  path.
- Button XAML verified by build + visual check per repo convention.
- Prior art: `MainViewModelTests` enrichment-refresh cases, `ServerEnrichmentServiceTests`.
- TDD per repo standard: RED → minimal GREEN → mandatory REFACTOR.

## Out of Scope

- Refreshing CFX service status (left panel) — that stays on the per-minute loop.
- Pull-to-refresh gestures, keyboard shortcuts, auto-refresh settings.
- Changing the per-minute cycle or TTL defaults.
- Refreshing servers in the browser view (phase 12 has its own button reusing this seam).

## Further Notes

- Depends on phase 9 (forced-refresh + cooldown seam). Implement after it.
