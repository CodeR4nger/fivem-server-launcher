# 02 — URL-keyed `GetIconAsync(iconUrl)` with validation + raised cap

Status: done

## What

`IServerEnrichmentService` gains `Task<byte[]?> GetIconAsync(string iconUrl)` —
single-argument, URL-validated, URL-keyed — replacing the (cfxId, version) shaped
flow at the seam level. The old two-argument method stays until 05 (its consumers
switch in 04).

Mechanics are unchanged from the version-keyed flow, with the URL as the whole cache
key: LRU cache, single-flight registered before the download starts, injectable
concurrency bound, `HttpRequestException`/`TaskCanceledException` → null, unexpected
faults propagate to joined callers.

## Why

The URL is self-versioned (a new icon = a new guid URL): it is a complete cache key,
and `cfxId` adds nothing. Validation before any network call is load-bearing beyond
hygiene: `HttpClient.GetAsync("file://…")` throws `NotSupportedException`, which is
outside the sanctioned swallow set and would fault every caller sharing the in-flight
download.

## Scope

- `src/FiveMServerLauncher/Service/ServerEnrichmentService.cs`:
  - `GetIconAsync(string iconUrl)`: validate → cache/in-flight lookup keyed by the
    URL string → bounded download from the *given* URL (no format string; the server
    publishes the complete URL).
  - Validation (pure, private): non-empty, absolute, scheme exactly `https`, host
    non-empty, no userinfo. Invalid → `Task.FromResult<byte[]?>(null)`, zero network.
  - `DefaultMaxIconBytes` 2 MB → 8 MB (observed real icons up to 5.07 MB).
  - Delete the `IconUrl` format-string constant with the old method (05 removes the
    method itself; keep it compiling until then).
- `tests/…/Service/FakeServerEnrichmentService.cs`: implement the new member
  (`LastRequestedIconUrl`), keep the old one until 05.
- `tests/…/Service/ServerEnrichmentServiceTests.cs`: rewrite the icon download
  claim-tests for the new seam under new names: cached by URL without re-download;
  different URL → new download; failure → null; concurrent same-URL requests share one
  download; distinct URLs bound in-flight downloads; LRU eviction; sync-completing
  download not parked in-flight; oversize (above the new cap) → null; validation
  matrix (`http://`, relative, `file://`, userinfo, garbage, empty → null with zero
  recorded requests via the routed handler's counters).

## Done when

- `GetIconAsync(iconUrl)` serves, caches and single-flights by URL, refuses invalid
  URLs without touching the network, and tolerates real-world icon sizes.
- The full suite is green.
