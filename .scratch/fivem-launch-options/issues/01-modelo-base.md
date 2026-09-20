# 01: Base FiveMLaunchOptions model with validated factory

**What to build:** the immutable domain value that describes how to launch FiveM for a given intent. It exposes `Address` (nullable: the connection address when connecting to a server; `null` when opening the client directly), `GameClient`, `GameBuild`, `PureMode` and `SecondClient`. A static factory builds the model validating its state (malformed address → error; invalid/unknown `GameClient` → error). It launches no processes and touches no files; it depends only on domain types.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Given a `FiveMLaunchOptions` built with known fields, it exposes `Address`, `GameClient`, `GameBuild`, `PureMode` and `SecondClient` with those values
- [x] It can be built without `Address` ("open directly" intent) without error
- [x] The factory rejects a malformed address (empty or without a server-address form)
- [x] The factory rejects an invalid `GameClient` (unknown enum value)
- [x] The value is immutable: assignments that would change it don't compile/fail at design time
- [x] No references to UI, services, or configuration; domain types only
