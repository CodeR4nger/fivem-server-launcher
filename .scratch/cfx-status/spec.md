# CFX service status (FiveM / FiveM Enhanced / RedM)

**Status:** done (tickets 01–04 resolved; suite 387 green; build clean; code review clean)

## Problem Statement

As a player, before connecting or launching, I want to know whether CFX services are healthy
for each game (FiveM, FiveM Enhanced, RedM), so I don't waste time debugging "is it me or is
CFX down?". The launcher currently hardcodes a static `FIVEM / CFX  ONLINE` block in the left
panel that always claims ONLINE — it carries no real information and can't tell me when CFX is
degraded or down.

## Solution

Replace the hardcoded status block with a live, honest three-row status display sourced from
the official public statuspage (`https://citizenfx.statuspage.io/api/v2/summary.json`,
already listed in DOC.md #Public status endpoints):

- One row per game — **FiveM**, **FiveM Enhanced**, **RedM** — each showing a colored dot and
  an English status label.
- Per-game component status is read from the statuspage's dedicated components for **FiveM**
  and **RedM**. The statuspage publishes **no dedicated FiveM Enhanced component**, so that row
  shows the **page-level (overall) CFX status indicator** instead — an honest "we only know
  CFX as a whole for Enhanced" fallback.
- Status refreshes on the **existing per-minute background loop** (one loop, not a second one):
  at startup and about once a minute while the window is open.
- On statuspage outage/parse failure the launcher **keeps the last known status** (or shows
  `UNKNOWN` when nothing was ever fetched) — never a crash, never a fabricated `ONLINE`.

## User Stories

1. As a player, I want the left panel to show the current CFX status for FiveM, FiveM
   Enhanced and RedM separately, so I can see at a glance whether the game I care about is
   healthy before I connect.
2. As a player, I want each game row to use an intuitive colored dot (green healthy, amber
   degraded, red outage, grey unknown), so I understand the situation without reading text.
3. As a player, I want a plain English status label per row (e.g. `OPERATIONAL`,
   `DEGRADED`, `OUTAGE`, `MAINTENANCE`, `UNKNOWN`), so the color is never the only signal.
4. As a player, I want FiveM Enhanced's row driven by the overall CFX status, so it still
   conveys real information even though CFX publishes no dedicated Enhanced component.
5. As a player, I want the status to refresh about once a minute with the saved-server
   enrichment, so it stays current without any action from me.
6. As a player, I want the status to refresh once at startup, so the block is never stale from
   a previous session's hardcoded claim.
7. As a player, when the statuspage is unreachable or returns garbage, I want the last known
   status kept (or `UNKNOWN` if none), so an outage of the statuspage doesn't masquerade as a
   CFX outage — and never crashes the launcher.
8. As a player, I want unmapped/unknown statuspage status strings to show `UNKNOWN`, so a new
   statuspage status value can't break or mislead the UI.
9. As a player, I want the status rows hidden while Dev Mode is active (same as the block they
   replace), so the layout rules don't change.
10. As a developer, I want the status fetch behind an injectable HTTP seam returning a typed
    dictionary (null on outage), so the behavior is fully testable without a real network.
11. As a developer, I want the refresh to reuse the existing enrichment loop seam (injectable
    cadence + delay), so no second timer/loop exists and the tests stay deterministic.
12. As a developer, I want the dot-color mapping as a data-only value converter (status →
    brush) and the label text owned by the view model, so no business logic lives in XAML.
13. As a player, I want the three rows to reuse the existing panel's typography/spacing, so
    the replacement looks native to the current design language.

## Implementation Decisions

- **New seam (the only one):** `ICfxStatusService.GetStatusesAsync()` →
  `IReadOnlyDictionary<GameClient, CfxStatus>?` (`null` = outage/parse failure; never throws;
  only `HttpRequestException`/`TaskCanceledException`/`JsonException` swallowed, mirroring
  `ServerEnrichmentService`). Real impl `CfxStatusService` takes an injectable `HttpClient`
  and performs **one GET of `summary.json` per call — no internal cache and no `TimeProvider`**:
  the VM's existing per-minute loop is the throttle (fewer moving parts; cache would be a
  second, unused source of staleness).
- **New status vocabulary:** enum `CfxStatus` =
  `Operational | Degraded | PartialOutage | MajorOutage | Maintenance | Unknown`.
  Statuspage component strings map: `operational`→`Operational`,
  `degraded_performance`→`Degraded`, `partial_outage`→`PartialOutage`,
  `major_outage`→`MajorOutage`, `under_maintenance`→`Maintenance`, anything else → `Unknown`.
  Page-level `status.indicator` maps: `none`→`Operational`, `minor`→`Degraded`,
  `major`→`MajorOutage`, `critical`→`MajorOutage`, anything else → `Unknown`.
- **Component mapping:** FiveM ← its dedicated statuspage component, RedM ← its dedicated
  component (component ids are constants in the service, resolved by component `name` lookup
  first so a regenerated id doesn't silently break the mapping; id as fallback), **FiveM
  Enhanced ← page-level `status.indicator`** (no dedicated component exists — user decision).
  A missing component for FiveM/RedM yields `Unknown` for that row, never an exception.
- **`MainViewModel`:** gains `ICfxStatusService`, a bindable `ObservableCollection`-free
  fixed trio exposed as `FiveMStatus`/`FiveMEnhancedStatus`/`RedMStatus` (each with
  `StatusLabel` text + `CfxStatus` value + `PropertyChanged` wiring), and a public
  `RefreshCfxStatusAsync()` called from the **existing** `RunEnrichmentLoopAsync` body (one
  loop, alongside `RefreshServerInfoAsync`) and once from `InitializeAsync`. `null` result →
  no update (keep last known); the very first fetch failing → `Unknown`.
- **UI (`MainView.xaml`):** the hardcoded `STATUS FIVEM` block (dot + `ONLINE`) is replaced by
  a three-row `ItemsControl`/`StackPanel` bound to the three status properties; each row =
  colored `Ellipse` + game name + status label. Visibility rules unchanged (hidden in Dev Mode,
  `InverseBoolToVisibility`). New **data-only** `CfxStatusToBrushConverter`
  (`Operational`→green, `Degraded`→amber, `PartialOutage`/`MajorOutage`→red,
  `Maintenance`/`Unknown`/default→grey) reusing existing brush resources; the English label
  text is computed in the VM, not in the converter.
- Composition root wires one `CfxStatusService(httpClient)` sharing the app's `HttpClient`.
- No new timers, no settings, no persistence: status is session-only display state.

## Testing Decisions

- Only external behavior is asserted (fetch → dictionary shape, refresh → VM state, status →
  label/brush); no test asserts internal parsing loops.
- **`CfxStatusService`** (prior art: `ServerEnrichmentService` tests, `FakeHttpMessageHandler`
  / `RoutedHttpMessageHandler`): maps dedicated component strings to per-game statuses; page
  indicator → `FiveMEnhanced`; unknown status strings → `Unknown`; missing component → that row
  `Unknown`; HTTP error / malformed JSON → `null`, never throws; cancel → `null`.
- **`MainViewModel`** (prior art: enrichment loop tests with `delay` seam and
  `FakeServerEnrichmentService`): refresh applies statuses to the three bindable properties and
  their `StatusLabel`s; `null` refresh keeps last known; first-ever failure shows `UNKNOWN`;
  the loop still calls status refresh each cycle and survives a throwing status service; all
  three labels correct for each `CfxStatus` value.
- **Converter**: status → expected brush for every enum member + unknown int → grey fallback
  (prior art: `GameClientToBrushConverter` tests).
- No real network, no real sleeps anywhere.

## Out of Scope

- Caching layer / `TimeProvider` for the status service.
- Incidents list, uptime history, per-component graphs, statuspage `statuspage`-only widgets.
- Persisting last status across restarts.
- A second background loop or a separate refresh cadence from enrichment.
- Any change to the saved-server row status (`players/max`/`OFFLINE`/`UNRESOLVED`) semantics.

## Further Notes

- Statuspage payload verified live: components FiveM (`gh9dmv9xj3hk`) and RedM
  (`ytm3dswd81gl`) exist under the "Games" group; no FiveM Enhanced component exists (hence
  the page-level fallback, user-approved).
- Phase is expected to close with the repo workflow: `to-tickets` → TDD implementation →
  `code-review` → commit, then the roadmap entry is marked done.
