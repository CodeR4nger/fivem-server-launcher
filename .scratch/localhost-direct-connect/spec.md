# Feature: Localhost direct connect with id-first / direct-fallback resolution

Status: ready-for-agent

## Problem Statement

A user running a local FiveM server (the typical server-dev workflow) cannot reliably save and
connect from the launcher:

- Bare `localhost` (no port) is misclassified by address classification as a CFX server id. Saving
  it produces a broken id-keyed entry; connecting shows "Invalid address".
- When a server is reached via its CFX id (typed id/join URL, or an id captured on a saved row) and
  the CFX API cannot resolve it — server briefly delisted while restarting, CFX outage — the connect
  fails outright with "Invalid address" even though the server is directly reachable and the
  launcher already knows a direct address for it.

## Solution

Make localhost a first-class direct-connect address form and make the connect pipeline resilient:
resolve via the server id first (validated path, enrichment, requirements); when that resolution
fails, fall back to a direct connect using the saved server's direct address instead of failing.
The fallback applies to every saved server, not only localhost entries.

## User Stories

1. As a local server owner, I want to save `localhost` (no port) as a direct connect, so that it
   connects to the standard default port without me typing it.
2. As a local server owner, I want `localhost:30120` and `127.0.0.1:30120` rows to keep connecting
   directly, so that my unlisted dev server still works (regression coverage for existing behavior).
3. As a local server owner, I want my saved localhost row to capture the server id when my server
   is publicly listed, so the row shows name/players/icon enrichment like any other saved server.
4. As a player, when I connect via a server id and the CFX API is unreachable, I want the launcher
   to fall back to the saved direct address, so that I still join the server.
5. As a player, when the server is briefly delisted (restart window), I want the same fallback, so
   that connect survives the gap.
6. As a player, when id resolution fails and no saved server links that id to a direct address,
   I want the existing "Invalid address" failure, so that bad input is still rejected.
7. As a player, I want the fallback profile to honor my saved server's manual Steam/Discord flags,
   so that required apps are still prepared before launching.
8. As a player selecting a saved row whose address is direct (ip:port, domain:port, localhost),
   I want connect to behave exactly as today, so that nothing regresses.
9. As a player typing `LOCALHOST` or other casing variants, I want the same classification, so
   that address matching is case-insensitive as everywhere else.
10. As a player typing a bare id or join URL that matches a saved row's captured id, I want the
    saved row's direct address used as fallback, so that id-form connects also survive outages.
11. As a player, when the fallback runs I want the connect to use the raw saved address (no
    invented fields), so that the unvalidated direct connect is honest about what it knows.
12. As a player, when the id resolves normally, I want no behavior change, so that validated
    connects keep their requirements, build, and pure-level handling.
13. As a player saving a server whose address is a cfx form (id or join URL), I understand no
    direct address is known, so a failed id resolution still fails as today.

## Implementation Decisions

- Address classification: bare `localhost` (exact, case-insensitive) classifies as a port-less
  domain (default-port semantics like bare IPs/domains). Ported `localhost:<port>` already
  classifies as domain:port today and stays unchanged. Loopback IPv4 forms are already valid and
  unchanged. The dotless/colonless token fallback that currently yields CfxId must not change for
  any other token.
- Saved-context matching stays in the main view model (VM-owned, per decision): connect resolves
  the saved server by typed address first; when the typed address is a cfx form and no
  address match exists, it looks the saved server up by cfx id.
- The server repository interface gains a find-by-cfx-id lookup (case-insensitive id match).
  The file-backed and in-memory repositories implement it; no persistence schema change (the id
  is already stored on saved servers).
- The resolver keeps its existing two-argument resolve contract: when the CFX service yields no
  server for an id-form address and the passed saved server's address classifies as a direct
  connectable form, return an unvalidated profile carrying the saved direct address. Manual
  requirement flags merge into that profile exactly as for any other connection. Otherwise the
  resolver throws the existing invalid-address exception.
- The fallback profile is unvalidated (not CFX-validated), has no invented project/game fields,
  and its address is the saved server's raw stored address.
- Fallback triggers only on failed resolution, not on launch failure.
- No UI copy changes in this feature (localization handles wording).

## Testing Decisions

- Good tests assert external behavior only: classification results, resolved profile shapes
  (validated vs unvalidated, address, effective requirements), connect outcomes and status text.
- Modules under test: address classification (pure static, existing tests), the resolver (with the
  existing fake CFX service / catalog / DNS collaborators), the server repositories (file-backed
  via temp files, in-memory for VM tests), and the connect flow in the main view model (fake
  resolver/launcher/preparer collaborators, asserting the launch URI and status).
- New repository behavior (find by id) gets direct repository tests: hit, miss, case-insensitivity,
  and consistency with the same-id-multiple-addresses reality (first match is acceptable and
  asserted as such).
- Prior art: existing resolver, address, repository, and view-model test suites; no new test
  doubles beyond what those suites already use.

## Out of Scope

- IPv6 loopback (`[::1]`) and IPv6 address forms generally.
- Special-casing LAN/private ranges (they already work as direct addresses).
- Fallback on launch failure (only resolution failure falls back).
- Any UI string changes.
- Changes to how ids are captured at save time (existing capture flow already covers localhost
  rows; tests only pin it).

## Further Notes

- Order: this is the first v1.2 feature; the security/performance sweep later reviews its code.
- The misclassification fix means previously-saved broken bare-`localhost` entries (stored as
  id-keyed) remain loadable but should be edited by the user; no data migration is added for them
  (corrupt/unknown entries already degrade safely).
- Amendment (ticket 05): publicly listed *hidden* servers can publish raw loopback endpoints
  (`127.0.0.1:30120`, e.g. cfx id `8y6354`). Those are never the user's own local server: catalog
  address matching ignores hidden endpoints (sentinel and loopback), so a typed localhost address
  always connects directly rather than hijacking a stranger's hidden server. A "listed local
  server" therefore only validates when listed under a matchable (non-loopback) endpoint.
- Amendment (ticket 06): saved loopback servers can carry manual connection parameters (sub Dev
  Mode): a loopback-only dialog section collects an optional cfx id, game build, pure mode, and
  game client on the saved server. At connect, a resolvable manual id wins over the manual values
  (requirements and game client from that exact listed server — never "the first match in the
  list"); a delisted id or no id applies the manual values; the connect stays a direct connect to
  the loopback address in every case, so local dev servers connect even while delisted.
