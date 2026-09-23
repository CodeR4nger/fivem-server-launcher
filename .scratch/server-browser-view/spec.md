# v1.1 Phase 12 — Server browser view

Status: ready-for-agent

## Problem Statement

The launcher can only reach servers you already know about (an address you paste or a server
you saved). There is no way to discover servers: browse the public CFX list, filter by game
or fullness, search by name — all of which the data we already download supports.

## Solution

A new full-screen *view* of the app (not a separate window) that lists all public CFX
servers: icon, name, game, players/max. Filters by game (FiveM / FiveM Enhanced / RedM),
hide-full, hide-empty, and search by name. Each server offers Connect (runs the normal
connect flow by cfx id and returns to the main view) and Save (adds it to saved servers via
the existing add dialog, prefilled).

## User Stories

1. As a user, I want to open a server browser from the main view, so that I can discover
   servers without knowing an address.
2. As a user, I want it to be a view of the same app (not another window), so that the
   experience stays single-window and consistent with Dev Mode's pattern.
3. As a user, I want to go back to the main view easily, so that browsing never strands me.
4. As a user, I want to see each server's icon, name, game, and player count, so that I can
   judge it at a glance.
5. As a user, I want to filter by game (FiveM / FiveM Enhanced / RedM), so that I only see
   servers for the client I play.
6. As a user, I want to hide full servers, so that I only see joinable ones.
7. As a user, I want to hide empty servers, so that I only see populated ones.
8. As a user, I want to search by name (live, case-insensitive), so that I can find a
   specific community quickly.
9. As a user, I want filters and search to combine (AND), so that I can narrow down in one
   pass.
10. As a user, I want to connect to a listed server, so that the browser is an entry point,
    not a dead end.
11. As a user, I want the connect action to run the identical flow as the main view
    (requirements preparation, status messages), so that there is one connect behavior.
12. As a user, I want to save a discovered server to my saved list, with the dialog prefilled
    (name from the server, address from its endpoint), so that I can build my list from the
    browser.
13. As a user, I want a server I've already saved to be visibly marked, so that I don't add
    duplicates.
14. As a user, I want a manual refresh that fetches fresh data (sharing the saved-list
    refresh cooldown), so that I can update the list on demand without hammering the CFX
    endpoint.
15. As a user, I want a catalog outage to show an honest empty/error state instead of a
    crashed or frozen view, so that failures are understandable.
16. As a user, I want the list to stay responsive with tens of thousands of servers, so that
    scrolling never stutters.

## Implementation Decisions

### View mechanics

- A new view (UserControl style, consistent with `MainView`) swapped over the **entire
  content area** of `MainWindow`, using the existing boolean-visibility swap pattern (same
  family as Dev Mode / settings / dialog overlay, one level up). No new `Window`.
- `MainViewModel` owns an `IsServerBrowserOpen` flag; a "BROWSE SERVERS" button on the main
  view opens it and a back affordance (and/or the same button inverted) closes it. While the
  browser is open, the main view's other overlays (settings, dev mode, server dialog) are
  closed.
- The browser has its own ViewModel (`ServerBrowserViewModel`) created at the composition
  root, sharing the *same* `ServerCatalog` instance (no duplicate cache) plus the enrichment
  service (icons), the saved-servers repository (saved-state + save action), and a connect
  callback into `MainViewModel`/the existing connect pipeline.

### Data

- Source: `ServerCatalog.GetSnapshotAsync()` (TTL-respecting on open). Snapshot rows map to a
  `ServerBrowserItem` (CfxId, display name from `sv_projectName` fallback `hostname` with CFX
  color codes (`^N`) stripped, `GameClient` from `gamename`, players/max from
  `clients`/`svMaxclients`, connect endpoint for saving).
- Entries whose only endpoint is the hidden `https://private-placeholder.cfx.re/` sentinel
  are included (they're connectable by cfx id) but their Save action stores the cfx-join form.
- Icons: reuse `IServerEnrichmentService.GetIconAsync(cfxId)` — but only fetched for visible
  rows (virtualized), never bulk-downloaded for 33,900 servers. Failure → no icon.
- Filtering: `ICollectionView` filter over the item collection, same pattern as phase 11.
  Game filter: single-select plus "All" (three games + all = four states); hide-full:
  `players >= max` excluded; hide-empty: `players == 0` excluded; search: case-insensitive
  substring on display name. All combine as AND.
- Virtualization on (WPF default for ItemsControl with VirtualizingStackPanel) to keep 33k
  rows responsive; rows are fixed-height to make virtualization effective.
- Manual refresh button in the browser uses the phase 9/10 forced-refresh + shared cooldown;
  while open there is no auto-refresh loop (the per-minute global loop already keeps the
  catalog warm — the browser does not add a second one).

### Connect

- Connect runs `MainViewModel`'s existing connect pipeline with the server's **cfx id**
  (the canonical join form) as the address: identical requirements preparation, status text,
  and `LastServerAddress` persistence.
- On connect, the browser view closes and the user sees status on the main view.
- Servers that publish `SteamRequired`/`DiscordRequired` flow through the existing
  effective-requirements machinery — no special-casing in the browser.

### Save

- Save from a browser row opens the *existing* add-server dialog (main view's overlay),
  prefilled: name = server display name, address = the catalog endpoint (raw `ip:port` /
  hostname endpoint / cfx-join form for hidden ones). The phase-9 save flow (validation,
  duplicate check, immediate refresh, background CfxId capture) applies unchanged.
- Duplicate handling: if the server is already saved (by cfx id or address), the row shows a
  saved marker and Save does nothing (or opens the edit dialog prefilled — decision: open the
  dialog prefilled, letting the user adjust; duplicate-adding is impossible because the
  repository rejects it).

### Composition root

- `App.xaml.cs` constructs `ServerBrowserViewModel` alongside `MainViewModel` (shared
  catalog/enrichment/repository instances) and wires the open/close + connect + save
  callbacks. No DI container, per convention.

## Testing Decisions

- `ServerBrowserViewModel` tests: snapshot → items mapping (name cleanup strips `^N` codes,
  `sv_projectName` fallback to `hostname` including color-code stripping, game mapping via
  existing `CfxVars`, players/max), each filter in isolation and combined, live name search,
  hidden-sentinel rows' save form, saved-marker state (repository fake), connect delegates
  with the cfx id, refresh delegates to the forced path, outage (null snapshot) → empty
  state, never throws.
- Reuse fakes: `RoutedHttpMessageHandler`, `TestProtobufFrames` (extended with
  projectName/banner vars as needed), `InMemoryServerRepository`, `FakeServerEnrichmentService`.
- Connect/save integration with `MainViewModel`: test at the VM-to-VM callback seam (browser
  raises "connect this cfx id"; main VM runs the same path as a typed cfx id — prior art:
  existing connect tests).
- XAML verified by build + visual check, per convention.
- TDD per repo standard: RED → minimal GREEN → mandatory REFACTOR.

## Out of Scope

- Sorting by columns (players, name, etc.), favorites, join history, server detail pane,
  banners/descriptions/tags/locale filters, ping, server country.
- A separate window (explicitly rejected — this is a view swap).
- Auto-refresh loop inside the browser.
- Bulk icon downloads.
- Editing saved servers from the browser (saving only).

## Further Notes

- Depends on phases 9 (address matching used when saving hostname-form endpoints; forced
  refresh) and 10 (shared cooldown semantics).
- The catalog is ~20 MB/33.9k servers; snapshot parsing already exists and is cached. Memory
  is fine; the only perf-sensitive points are virtualization and icon laziness, both called
  out above.
