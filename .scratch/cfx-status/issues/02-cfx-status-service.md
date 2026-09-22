# 02: CfxStatusService (HTTP seam, real impl)

**Type:** task

**What to build:** `Service/CfxStatusService : ICfxStatusService`. One GET of
`https://citizenfx.statuspage.io/api/v2/summary.json` per call, injectable `HttpClient`, no
internal cache/`TimeProvider` (VM loop is the throttle). Parse components; FiveM/RedM by
component name (id fallback); FiveMEnhanced from `status.indicator`. Swallows only
`HttpRequestException`/`TaskCanceledException`/`JsonException` → `null`; other exceptions
propagate (same policy as `ServerEnrichmentService`/`ServerCatalog`).

**Blocked by:** 01.

**Status:** resolved

- [x] Real `CfxStatusService` wired with `HttpClient`, parses real `summary.json` shape.
- [x] HTTP error / malformed JSON / cancellation → `null`, never throws.
- [x] Missing FiveM/RedM component → that entry `Unknown`; missing indicator → Enhanced `Unknown`.
- [x] Service tests via `FakeHttpMessageHandler`/`RoutedHttpMessageHandler` (api/v2 summary.json
      fixture) — prior art: `ServerEnrichmentServiceTests`.