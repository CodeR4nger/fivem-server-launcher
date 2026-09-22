# 02: Portable data directory + one-time migration

**What to build:** the launcher reads and writes its settings (`launcher-settings.json`) and
saved servers (`saved-servers.json`) **beside the running exe** instead of
`%localappdata%\FiveMServerLauncher`. The data directory is owned by a `PortableDataDirectory`
seam (injectable process-path; default `Environment.ProcessPath`), the composition root uses it,
and on first run with an empty portable dir it best-effort copies any existing `%localappdata%`
files over (portable wins on conflict, old files untouched as a backup).

**Blocked by:** None (can start immediately).

**Status:** resolved (suite 437 green; composition root uses seam + migration)

- [x] `PortableDataDirectory` resolves the exe's folder from the injected process path and composes the two unchanged file names; tests cover path resolution and the null/whitespace fast-fail (incl. `Default()` factory).
- [x] The one-time migration copy decision is covered by a focused test using temp directories.
- [x] The composition root no longer uses the hardcoded `%localappdata%\FiveMServerLauncher` path — it passes the seam's resolved paths to the existing storage classes, and runs the one-time migration first.
- [x] Build clean; suite 437 green (+12 tests).