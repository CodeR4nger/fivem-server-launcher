# 01: Direct tests for CfxStatusItem

**Type:** feature

**What to build:** a `CfxStatusItemTests` coverage for `ViewModels/CfxStatusItem`
— `DisplayName` per `GameClient` (via `InstalledClientOption.DisplayNameOf`),
`Status` get, `StatusLabel` text for every `CfxStatus` value, `Apply` setting
status and raising `PropertyChanged` for both `Status` and `StatusLabel`, and
`Apply` with an identical status raising nothing (equality short-circuit).

**Blocked by:** — (starts the phase)

**Acceptance:** the new tests follow the repo Given/When/Then convention and
target only `CfxStatusItem` (no fake seams needed — it is standalone).

**Status:** resolved

- [x] RED: expectations written against the public type (characterization; behavior already implemented)
- [x] GREEN: 13 tests pass; suite unaffected elsewhere
- [x] REFACTOR: none needed (standalone class)