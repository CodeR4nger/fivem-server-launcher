# 03: MainViewModel status refresh (VM + loop)

**Type:** task

**What to build:** `MainViewModel` gains `ICfxStatusService`, three bindable properties
`FiveMStatus`/`FiveMEnhancedStatus`/`RedMStatus` (each exposing a `CfxStatus` value + an English
`StatusLabel` like `OPERATIONAL`/`DEGRADED`/`OUTAGE`/`MAINTENANCE`/`UNKNOWN`, with
`PropertyChanged` wiring), and `RefreshCfxStatusAsync()`. Called once from `InitializeAsync` and
each cycle of the **existing** `RunEnrichmentLoopAsync` body. `null` result → no update (keep
last known); first-ever failure → `UNKNOWN`.

**Blocked by:** 02.

**Status:** resolved

- [x] Three bindable status properties + English `StatusLabel` per `CfxStatus` value.
- [x] `RefreshCfxStatusAsync` applies fetched statuses; `null` keeps last known; first failure
      yields `UNKNOWN`.
- [x] `InitializeAsync` refreshes status once at startup (alongside open-client seeding).
- [x] `RunEnrichmentLoopAsync` calls status refresh each cycle; survives a throwing status
      service (loop never dies), same seam pattern as the enrichment refresh.
- [x] VM tests with a fake `ICfxStatusService` (following `FakeServerEnrichmentService`), no
      real network/sleeps.