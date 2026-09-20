# 04: Temporary file fixture in FileSettingsStorageTests

**What to build:** removes duplication in `FileSettingsStorageTests` by extracting a reusable fixture that creates (and cleans up) a temporary directory for each test. The full save/load scenario is also reused instead of being repeated between `SaveAndLoad` and `Load`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] A helper/fixture provides a unique temp path per test and deletes it on teardown
- [x] The 9 uses of the temp-dir + `try/finally Delete` pattern consume it
- [x] The SaveAndLoad↔Load duplication is reduced by sharing the scenario
- [x] Suite green with no regressions
