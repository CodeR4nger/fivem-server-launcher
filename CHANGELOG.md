# Changelog

## [1.2.0] (unreleased)

### Added
- **UI localization** — 11 languages, defaults to the system language, live switching from Settings, English fallback guaranteed.
- **Localhost support** — `localhost`/`127.0.0.1` work as direct connect addresses; saved loopback servers can carry manual parameters (CFX ID, game build, pure mode, game) in a dedicated local-dev panel, with a resolvable CFX ID winning over manual values.
- **Resilient id connects** — if a cfx id can't be resolved (server delisted, CFX outage), the launcher falls back to the saved direct address instead of failing.

### Changed
- **Hidden servers are never matched by address** — typing a local address connects to your server, not a stranger's hidden one.
- **Bounded network waits** — 15 s HTTP, 5 s DNS, capped download sizes; the UI can no longer freeze for minutes.
- **Atomic writes** — saved servers, settings and `CitizenFX.ini` can no longer be corrupted (or wiped) by a crash mid-write.

### Fixed
- A malformed server response could crash the launcher.
- A disk fault during the background id capture could fail a server save.
- Startup survives installation/IO faults with a status message instead of crashing.

### Security
- Command-injection hardening: strict cfx-id charset everywhere, manual ids validated on save, escaped executable paths — crafted `saved-servers.json` entries can no longer smuggle shell arguments.
