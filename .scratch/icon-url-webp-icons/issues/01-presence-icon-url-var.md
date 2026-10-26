# 01 — Read `icon_url` from catalog vars onto the presence

Status: done

## What

The presence dictionary built by `ServerEnrichmentService.RefreshAsync` carries the
new icon fact: `ServerPresence.IconUrl` (nullable string), read from the catalog
server's `vars["icon_url"]` through a new `CfxVars.TryGetIconUrl(IDictionary<string,
string>)` — same shape as `TryGetGameClient`.

The dead typed-field reader stays for now (deleted in 05, so this ticket stays a
vertical slice): `ServerPresence.IconVersion` and `CfxVars.TryGetIconVersion` keep
compiling until the consumers switch in 04.

## Why

The typed `iconVersion` proto field is no longer published by any live server
(0/33,548 verified live); `icon_url` in vars is the only remaining source of icon
truth.

## Scope

- `src/FiveMServerLauncher/Service/CfxVars.cs`: add `TryGetIconUrl`.
- `src/FiveMServerLauncher/Service/ServerEnrichmentService.cs`: `ServerPresence`
  gains `IconUrl` (keep `IconVersion` temporarily); `RefreshAsync` populates it from
  `server.Data.Vars` via `CfxVars.TryGetIconUrl`.
- `tests/…/Service/ServerEnrichmentServiceTests.cs`:
  - Rewrite the iconVersion presence claim-test under a new name asserting the
    `icon_url` var lands on `IconUrl` (present var → value; absent var → null;
    empty-string var → null).
  - Delete `RefreshAsync_WhenIconVersionPublishedInSnapshot_ShouldExposeItOnPresence`
    (its claim is now false) and rewrite its live-truth under the new name.
  - Existing tests constructing `ServerPresence` may need the new parameter.

## Done when

- A presence built from a catalog frame carrying `icon_url` exposes it on
  `IconUrl`; frames without the var expose null.
- The full suite is green and no test asserts the typed-field claim anymore.
