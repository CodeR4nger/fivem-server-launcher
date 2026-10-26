# 04 — Consumers switch to `IconUrl`

Status: done

## What

Every consumer of the old icon fact switches to the new one; the icon pipeline runs
end to end on `icon_url`:

- `MainViewModel.RefreshServerInfoAsync`: gate on `serverPresence.IconUrl is null`
  → `continue`; fetch via the URL-keyed `_enrichment.GetIconAsync(iconUrl)`.
- `ServerBrowserItem`: `IconVersion` property → `IconUrl` (set in the private ctor);
  `FromServer` reads `CfxVars.TryGetIconUrl(data.Vars)`.
- `ServerBrowserViewModel.LoadIconAsync`: gate on `item.IconUrl`, fetch by URL.
- `FakeServerEnrichmentService`: consumers recorded via `LastRequestedIconUrl`.

## Why

The typed field is dead (0/33,548 live servers) — every path reading it silently
produces "no icon". This ticket moves the runtime onto the live source of truth.
It ships after 01–03 so the switch is a pure consumer change.

## Scope

- The three consumer files above; no XAML changes (row `Icon` byte bindings are
  unchanged).
- View-model tests rewritten under new names where their claims died:
  - `tests/…/ViewModels/MainViewModelTests.cs`: presence-with-`IconUrl` → icon fetch
    by URL + `SetIcon`; presence-without → no fetch (delete the iconVersion-named
    tests).
  - `tests/…/ViewModels/ServerBrowserViewModelTests.cs`: catalog frames with the
    `icon_url` var produce rows that request their icon; frames without the var never
    request (delete/rewrite the `RowIcon_WhenCatalogPublishesIconVersion…` pair and
    the `server.Data.IconVersion = 55` fixtures).
- `SavedServerItem` needs no change: `ApplyPresence`/`SetIcon` are byte-based.

## Done when

- The whole icon flow — catalog var → presence/item → URL-keyed download → normalized
  PNG → row bytes — works without touching the dead field, and the full suite is
  green.
