# v1.1 Phase 9 — Improvements & fixes: panel layout, dialog field titles, port-less addresses, save-triggers-refresh

Status: ready-for-agent

## Problem Statement

Four papercuts from real use:

1. The saved-servers list is tiny (`MaxHeight=110`, ~3 rows) while the connect-address area
   below it wastes vertical space (large text box, big gap before the ENTER SERVER button).
2. The add/edit server dialog shows two bare text boxes with no labels — it's not obvious
   which box is the name and which is the address.
3. Addresses without a port (e.g. a reverse-proxied `play.example.com`) are rejected as
   invalid, and even `domain:port` matching misses servers whose catalog endpoint is a proxy
   URL string rather than the resolved IP.
4. After saving a server in the dialog, its row shows no presence/icon until the per-minute
   enrichment cycle gets around to it.

## Solution

1. Compact the connect-address area (smaller/prettier text box, removed dead space) and give
   the reclaimed height to the saved-servers list.
2. Small titles above each box in the add/edit dialog: NAME and ADDRESS.
3. Accept port-less addresses; resolve and match them using data-driven catalog rules (see
   Implementation Decisions — the rules come from a spike against the real snapshot).
4. Saving a server refreshes its server data immediately.

## User Stories

1. As a user with several saved servers, I want to see more of them at once in the list, so
   that I don't have to scroll for the common case.
2. As a user, I want the connect address box to be compact, so that the window feels balanced
   and the list gets the space.
3. As a user adding a server, I want labels telling me which box is the name and which is the
   address, so that I don't mix them up.
4. As a user, I want to paste a server's address without a port (e.g. `play.example.com`),
   so that I can connect to reverse-proxied servers that don't publish a port.
5. As a user, I want a port-less address to resolve to the real CFX server when it's in the
   catalog, so that I get validated requirements (game build, pure mode, Steam/Discord) and
   enrichment (players, icon) just like a cfx id.
6. As a user, I want a port-less address that isn't in the catalog to still connect, so that
   unlisted/private servers remain reachable (FiveM defaults the port to 30120).
7. As a user, I want bare IPs without a port to work too, so that quick-paste addresses are
   accepted.
8. As a user, I want my `domain:port` addresses to match servers whose catalog endpoint is a
   proxy URL (e.g. `https://play.example.com:443/`), so that proxied servers get validated
   profiles instead of silently degrading.
9. As a user, I want a newly saved server to show its players/icon/status right away, so that
   saving feels immediate and correct.
10. As a user, I want invalid input to still be rejected with "Invalid address", so that
    typos don't produce silent misbehavior.

## Implementation Decisions

### Layout (9a)

- Keep the window fixed at 860×520 NoResize — no resizing, no restructuring of the right
  column's ScrollViewer/StackPanel.
- Compact the connect-address TextBox: smaller font (13 down a notch), tighter padding,
  lower `MinHeight`, and remove the wasted margins between the "CONNECT ADDRESS" header, the
  box, and the ENTER SERVER button.
- Raise the saved-servers ListBox `MaxHeight` by the reclaimed space (from 110 upward; final
  number tuned to the compacted address area so the full right column doesn't scroll in the
  common case).

### Dialog titles (9b)

- Add small caption TextBlocks ("NAME", "ADDRESS") above the two dialog TextBoxes, consistent
  with existing header styling. No changes to the dialog ViewModel surface.

### Port-less addresses & catalog matching (9c)

Spike facts (read-only analysis of the real streamRedir snapshot, 33,900 servers,
September 2026; dump kept in the agent temp dir):

- ~39% of endpoints use a port other than 30120 — never assume 30120 for *matching*.
- ~2.3% of servers publish a proxy-URL endpoint (e.g. `https://play.unityrp.io:443/`); ~7%
  publish only the hidden `https://private-placeholder.cfx.re/` sentinel (join-by-cfx-id only,
  nothing we can do).
- Zero IPv6 endpoints; exactly one endpoint per server in practice.
- DNS-resolving a proxied domain matches catalog IPs only ~3% of the time — DNS-IP matching
  does NOT work for the reverse-proxy case. The proxy hostname itself IS in the catalog as a
  URL string.
- The same IP can host 40–60 distinct servers (shared hosting) — matching by IP *without* a
  port is ambiguous and forbidden.

Decisions:

- `ServerAddress` gains two new kinds for port-less forms (bare IPv4, bare domain). Both are
  valid and connectable. Validation reuses the existing octet/label rules, just without the
  port requirement. Bare `cfx.re/join/<id>` handling is unchanged, and the no-dot/no-colon
  `CfxId` rule keeps precedence.
- New default-port constant (30120) lives in the domain layer, single definition.
- Catalog endpoint normalization for matching: strip scheme (`https://`), trailing slash, and
  a `:443` port from URL-form endpoints to compare host parts. This is a matching concern, not
  a display concern — never rewrite the user's address.
- Resolution rules (in `ServerResolver`):
  - Bare domain: (a) string-match the domain against normalized catalog endpoint host parts
    (unique match required for enrichment-key certainty; a validated profile is still built
    from whichever single server holds it — multiple servers sharing one proxy hostname with
    no port distinguishing them are resolved to the unique match when there is exactly one,
    otherwise the connectable fallback); (b) if no hostname match, DNS-resolve and exact-match
    `resolvedIP:30120`; (c) else unvalidated connectable profile, address used as-is (no port
    in the connect URI — FiveM defaults 30120).
  - Bare IP: exact-match `ip:30120`; else unvalidated connectable profile, address as-is.
  - `domain:port` (existing): gains rule (a) hostname string-match before/alongside the
    existing DNS→`ip:port` and raw-string matches. Ordering and dedup keep one profile.
- `FiveMLaunchOptions`/`ToUri()` accept the new forms: cfx id when validated, raw address
  (possibly port-less) otherwise.
- `SavedServer.Create` accepts the new kinds (so they can be saved); `CfxId` capture
  (`ServerEnrichmentService.ResolveCfxIdAsync`) supports them via the same matching rules.
- This fix touches Domain + Service seams only; no changes to persistence format, no new
  settings fields.

### Save triggers refresh (9d)

- Forced-refresh seam: `ServerCatalog` gains a way to bypass/invalidate its TTL for the next
  fetch; `IServerEnrichmentService` gains a forced variant (e.g. `RefreshAsync(force: true)`)
  that passes it through. Icons keep their (cfxId, iconVersion) cache semantics.
- Shared cooldown: the enrichment service refuses forced fetches more often than a short
  interval (target ~15 s, injectable for tests); a refused force simply serves the cached
  snapshot (never throws, never blocks).
- `SaveServerDialogCommand`'s flow calls a forced `RefreshServerInfoAsync()` after the row is
  added/updated (fire-and-forget is acceptable only if it can't race the dialog close —
  prefer awaiting before "Server saved" status, since the refresh is fast and user-expected).
- The per-minute loop keeps its normal (non-forced) cadence and never counts against the
  cooldown.

## Testing Decisions

- All new behavior is tested at existing seams: `FakeHttpMessageHandler` /
  `RoutedHttpMessageHandler` for catalog HTTP, `FakeTimeProvider` for TTL/cooldown,
  `FakeDnsResolver` for DNS, `InMemoryServerRepository`/`FakeServerEnrichmentService` for the
  ViewModel, `TestProtobufFrames` for catalog fixtures (extend with URL-form and
  non-30120-port endpoints).
- `ServerAddress` classification: pure unit tests, table-driven over the new/old forms
  (including `https://play.example.com:443/` shape inputs when users paste them).
- Catalog matching: normalization + host-part matching + exact `ip:30120` against decoded
  fixture frames. Ambiguity (two servers sharing one proxy hostname) is a test case: no
  invented enrichment.
- Resolver: matrix of {bare domain, bare IP, domain:port} × {hostname match, DNS+default-port
  match, no match} → validated vs unvalidated profile with correct `CfxId`/`Address`.
- Cooldown: forced refresh inside the window serves cache (request count stays 1); after the
  window, it refetches.
- ViewModel save flow metadata: save triggers exactly one forced refresh. XAML/layout changes
  (9a/9b) are verified by build + visual check, not unit tests, per repo convention.
- Prior art: `ServerAddressTests`, `ServerCatalogTests`, `ServerResolverTests`,
  `ServerEnrichmentServiceTests`, `MainViewModelTests` save-dialog cases.
- TDD per repo standard: RED → minimal GREEN → mandatory REFACTOR, one slice at a time.

## Out of Scope

- The refresh button and search bar UI (phases 10-11; the forced-refresh seam lands here and
  is shared).
- The server browser view (phase 12).
- Supporting the 7% `private-placeholder.cfx.re` hidden servers (impossible by address; cfx
  id already works).
- IPv6 endpoints (zero exist in the catalog).
- Window resizing / layout restructure beyond the compaction described above.
- Multiple endpoints per server (none exist in the snapshot).

## Further Notes

- Reverse proxies are why this matters: users getconnect URLs like `play.example.com` from
  Discord servers, and today the launcher flat-out rejects them.
- Empirical matching data (port distribution, proxy suffixes) lives in the spike report in
  the conversation history for phase 9; if rules need revisiting, re-run the spike against a
  fresh snapshot rather than guessing.
