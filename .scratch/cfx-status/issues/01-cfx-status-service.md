# 01: CfxStatus enum + ICfxStatusService seam

**Type:** task

**What to build:** new `Core/Enums/CfxStatus` (`Operational | Degraded | PartialOutage |
MajorOutage | Maintenance | Unknown`) and new `Service/ICfxStatusService` seam
(`Task<IReadOnlyDictionary<GameClient, CfxStatus>?> GetStatusesAsync()`; `null` = outage/parse
failure, never throws). Statuspage string mapping: `operational`→`Operational`,
`degraded_performance`→`Degraded`, `partial_outage`→`PartialOutage`, `major_outage`→`MajorOutage`,
`under_maintenance`→`Maintenance`, else → `Unknown`. Page `status.indicator`:
`none`→`Operational`, `minor`→`Degraded`, `major`/`critical`→`MajorOutage`, else → `Unknown`.

**Blocked by:** none.

**Status:** resolved

- [x] `CfxStatus` enum lives in `Core/Enums` (needs no namespace fighting with existing enums).
- [x] `ICfxStatusService` returns one entry per `GameClient`: FiveM/RedM from their dedicated
      statuspage components (component resolved by `name` first, id fallback), FiveMEnhanced
      from the page-level indicator. Missing component → that row `Unknown`, never throws.
- [x] Status string → `CfxStatus` mapping covered by unit tests for every documented value +
      unknown.
- [x] Indicator → `CfxStatus` mapping covered for `none`/`minor`/`major`/`critical`/unknown.