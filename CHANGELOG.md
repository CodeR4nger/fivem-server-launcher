# Changelog

## [1.3.0] (unreleased)

### Added
- **Reorder saved servers** — hover or select a row to reveal up/down arrows in the margin next to the list; or drag a row straight onto a new slot, guided by an accent drop line. The order persists across restarts and never applies while the search filter is active.

### Fixed
- Editing a saved server's address kept it in place on screen but scrambled its position after a restart.

## [1.2.0] (unreleased)

### Added
- **UI localization** — 11 languages, defaults to the system language, live switching from Settings, English fallback guaranteed. Adding a language is a single JSON file: its native name lives in the file rather than in code, so a community translation needs no source change.
- **Localhost support** — `localhost`/`127.0.0.1` work as direct connect addresses; saved loopback servers can carry manual parameters (CFX ID, game build, pure mode, game) in a dedicated local-dev panel, with a resolvable CFX ID winning over manual values.
- **Resilient id connects** — if a cfx id can't be resolved (server delisted, CFX outage), the launcher falls back to the saved direct address instead of failing.

### Changed
- **Hidden servers are never matched by address** — typing a local address connects to your server, not a stranger's hidden one.
- **Bounded network waits** — 15 s HTTP, 5 s DNS, capped download sizes; the UI can no longer freeze for minutes.
- **Atomic writes** — saved servers, settings and `CitizenFX.ini` can no longer be corrupted (or wiped) by a crash mid-write.
- **Enrichment request hygiene** — the server catalog is downloaded once and shared by every surface (connect, saved list, browser) instead of per-caller; the per-minute saved-list refresh no longer probes CFX per row (the catalog snapshot is the single source of icon versions), and icon downloads are shared, concurrency-bounded and LRU-cached — the launcher is a far better CFX API citizen.
- **Smooth server browser** — search filtering debounces while typing instead of re-filtering the 30k-row list per keystroke, saved-row markers use keyed lookups, and icons decode at a bounded pixel size with decoded-image reuse, so crafted server icons can no longer blow up memory.
- **Traditional Chinese systems read Simplified Chinese** — `zh-Hans` is the only Chinese dictionary we ship, so a `zh-TW`/`zh-HK` locale no longer falls through to English; shipping a Traditional dictionary later is a data-only addition.

### Fixed
- The language picker showed its names in the wrong language at startup when a non-English language was already saved.
- A saved browser game filter the launcher no longer offers could crash the browser at startup.
- A language file that fails to parse no longer silently vanishes from the picker — it stays selectable and renders through the English fallback.
- A malformed server response could crash the launcher.
- A disk fault during the background id capture could fail a server save.
- Startup survives installation/IO faults with a status message instead of crashing.
- Server icons load again in the browser and the saved list — the catalog publishes the icon version as a typed field (any non-zero value, including negative, is valid), not a server variable.
- Closing the window no longer runs one final refresh cycle; every cooldown and per-row enrichment step honors shutdown.
- Native handle leaks closed — process readiness probes (per poll), the URI scheme registry check (per connect) and shell-started processes no longer leak handles.
- Deleted/edited saved rows no longer stay in memory through the localizer's event; language switching is now safe for background reads by construction.
- An antivirus lock on `saved-servers.json` or `launcher-settings.json` degrades to empty/defaults instead of crashing startup.

### Security
- Command-injection hardening: strict cfx-id charset everywhere, manual ids validated on save, escaped executable paths — crafted `saved-servers.json` entries can no longer smuggle shell arguments.
- `CitizenFX.ini` injection guard — a malicious server can no longer smuggle keys or sections into the game config through a crafted pool-sizes value; poisoned entries are skipped instead of written.
