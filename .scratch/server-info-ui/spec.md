# Enriched saved-server rows (icon, players, online, game tag)

**Status:** done (tickets 01–05; suite 357 green; build clean; manual E2E verified)

## Problem Statement

Data sources already exist (`CfxService` `/single/`, `ServerCatalog` streamRedir, verified icon
endpoint) but saved-server rows show only a name (+ edit pencil). The roadmap's "Richer
server-info UI" is scoped down to exactly three fields per row: **server logo**, **players /
max players**, and **online status**, refreshed periodically. Because several servers may be
saved, the launcher needs to associate each saved server with its **CFX id** (endpoint-published
id) to look these up; servers without an obtainable CFX id simply show a plain (non-enriched)
row.

## Solution

- `SavedServer` gains an optional **`CfxId`** (`string?`, null when unknown). It is the
  address-keyed repository's new enrichment key, persisted via the existing JSON file (missing
  `CfxId` in old files → null → plain row).
- CFX id capture happens **at save**, in the background (save stays instant):
  - CFX-form address (bare id, `cfx.re/join/...`): `CfxId` = extracted id, synchronously known.
  - IP:port / domain:port: a best-effort background `ServerCatalog` lookup by ip-port matches the
    catalog `EndPoint` (= canonical cfx id) → stored via repository `Update`. Not found/offline at
    save time → `CfxId` stays null (no lazy re-capture; the user picked save-time capture).
- A per-minute enrichment cycle drives the rows (window open, minimized or not):
- One `streamRedir` catalog download → a `CfxId → (online, Players, MaxPlayers, Game)` map
    (`clients`/`sv_maxclients` fields and `gamename` var confirmed in the real payload).
    **Online = present in the catalog**. The `GameClient` (FiveM / FiveM Enhanced / RedM) is derived
    from the catalog `gamename` var — the row's per-game tag needs no extra HTTP.
- Per enriched server, `/single/{id}` is used to track `iconVersion`; the icon is fetched from
    `https://frontend.cfx-services.net/api/servers/icon/{id}/{iconVersion}.png` (verified 200 PNG)
    and **cached in memory keyed by (CfxId, iconVersion)** — re-download only when the version
    changes.
- Row rendering: `<ICON> <saved name> [gameTag] <status>` when enriched: the per-game colored
  tag (FiveM orange / FiveM Enhanced blue / RedM red) once the game is known; the status slot
  shows `players/max` when online, `OFFLINE` for a CfxId row not in the current catalog,
  `UNRESOLVED` when the row has no CfxId. Offline/unresolved rows never show invented numbers;
  the last known game is kept when a presence carries none.

## User Stories

1. As a player, I want each saved server to show a small logo, the current player count and the
   max player count, so I can pick a populated server without opening the client.
2. As a player, I want player counts and online status to refresh about once a minute while the
   launcher is open, so the list stays current without manual actions.
3. As a player saving an IP:port/domain server, I want the launcher to capture its CFX id in the
   background at save time, so the row becomes enriched without blocking the dialog.
4. As a player, I want servers whose CFX id can't be obtained (offline/unpublished at save time)
    to show an honest `UNRESOLVED` mark (and `OFFLINE` for a CfxId row not in the catalog), so
    nothing fabricated/incorrect is shown.
5. As a developer, I want the enrichment cycle, icon cache and cadence all behind injectable
   seams (HTTP, delay), so per-minute refresh is testable without real network.
6. As a player, I want a small colored tag right of each server name showing which game it runs
   (RedM, FiveM, FiveM Enhanced — each its own color), so I can pick the right server at a glance.

## Implementation Decisions

- `SavedServer`: add `CfxId` (`string?`, init/ctor, `[JsonConstructor]` param, `Create` overload
  accepting `cfxId`). Repository semantics unchanged (address remains the identity key; `Update`
  re-saves the enriched CfxId).
- New `Service/ServerEnrichmentService` (seam): owns catalog → id presence/players/max mapping
  (`RefreshAsync`) and icon version tracking + icon byte cache (`GetIconAsync`), injectable
  `HttpClient` + `TimeProvider` + catalog TTL. Never throws on outage (degrades to "no update").
- `SavedServerItem` (row VM) gains `CfxId`, `Players`/`MaxPlayers` (`int?`), `Online` (`bool`),
  `IconSource` (cached `BitmapImage`/bytes) with `PropertyChanged` wiring (existing
  `changeHandler` pattern).
- `MainViewModel` owns the refresh loop: injectable cadence (default 60 s) via a `delay` seam
  `Func<TimeSpan, CancellationToken, Task>` (delegates to `Task.Delay` in production; tests inject
  immediate yield or a controlled wait — simpler than a `TimeProvider` and not tied to
  `FakeTimeProvider`), runs while the window is open; saves the captured CfxId through the
  repository.
- Save flow: `SaveServerDialogCommand` stays sync; the IP:port/domain CfxId capture runs
  fire-and-forget after the row is added (no dialog block, no busy state).
- Servers are loaded at startup with `CfxId` from disk; rows without one are not enriched.

## Testing Decisions

- `ServerEnrichmentService`: fake HTTP → catalog frames map to presence/players/max + game;
  outage/corrupt frames/duplicate EndPoints → no update (never throws); icon fetched once per
  version, cached, re-fetched on version change; corrupt `/single/` payload → null, never throws.
- `SavedServer`/repository: CfxId round-trips through JSON; missing CfxId deserializes to null;
  `Update` preserves CfxId.
- `MainViewModel`: enrichment applied to rows after a refresh tick; servers without CfxId keep
  `UNRESOLVED`, CfxId rows not in the catalog show `OFFLINE`; captured CfxId persisted via
  repository `Update`; the enrichment loop survives a throwing refresh (never dies).
- Injectable cadence drives the refresh loop; no real sleeps/network in tests.

## Out of Scope

- Banner, news, Discord, website, upvote/power, CFX info (roadmap independent item's other
  fields).
- Auto-update (separate independent item).
- Enriching unvalidated (non-CFX) servers; lazy re-capture of CfxId after save.