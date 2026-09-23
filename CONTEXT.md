# CONTEXT

Glossary of the domain language of CFX Launcher. Implementation-detail-free: how things are
built lives in AGENTS.md / ADRs; what words *mean* lives here.

## Core terms

- **Saved server** — a server the user chose to keep, identified socially by its *address*.
  May carry manual Steam/Discord requirement flags.
- **Connectable address** — any user-typed or stored string the launcher accepts for
  connecting: cfx id, `cfx.re/join/<id>`, `ip:port`, `domain:port`, and (v1.1) port-less
  *bare IP* and *bare domain* forms.
- **Validated server** — a server found in the official CFX catalog, whose published
  requirements (game build, pure mode, Steam/Discord) the launcher trusts. An *unvalidated*
  server is still connectable, it just carries no published facts.
- **Enrichment** — the decoration of a saved server with live catalog facts: online/offline,
  players/max, game, icon.

## Catalog terms (from the streamRedir snapshot, v1.1 spike)

- **Catalog endpoint** — the address a server publishes for itself. Almost always exactly one
  per server. Three shapes exist: `ip:port`, proxy-URL (e.g. `https://play.foo.io:443/`), and
  the hidden-server sentinel.
- **Hidden server** — a server whose only catalog endpoint is the
  `private-placeholder.cfx.re` sentinel: reachable by cfx id only, never by address.
- **Default port** — 30120, the FiveM presumption when an address carries none. Used as a
  matching/connect fallback only; never assumed when matching the catalog (most non-default
  servers matter).

## v1.1 terms

- **Server browser** — a full content-area view of the same window (not a new window) that
  lists all public catalog servers with filters (game, hide full, hide empty) and name
  search, offering Connect and Save per server.
- **Forced refresh** — an on-demand enrichment update that bypasses the catalog's TTL cache,
  available to explicit user actions (save, refresh buttons).
- **Refresh cooldown** — a short minimum interval between forced refreshes, shared by every
  feature that can force one, protecting the expensive catalog download.
- **View swap** — the app's pattern for replacing one view with another in the single fixed
  window (Dev Mode, settings, dialog overlay, server browser).
