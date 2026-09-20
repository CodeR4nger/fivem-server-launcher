# 03: Open directly — ToCommandLineArgs()

**What to build:** the "open the client directly" intent (Dev Mode / open FiveM without a server, from preferences or manual overrides). Given a `FiveMLaunchOptions` without an address, `ToCommandLineArgs()` produces the list of command-line arguments to open the FiveM/RedM client: `-b<build>`, `-pure_<level>` and `-cl2` (only if `SecondClient`). For `FiveMEnhanced` it returns an empty list (that client doesn't support `-b`/`-pure_`/`-cl2`, verified). It never includes a server address.

**Blocked by:** 01 (Base FiveMLaunchOptions model with validated factory)

**Status:** resolved

- [x] With `GameBuild`, the args include `-b<build>`
- [x] With `PureMode`, the args include `-pure_<level>`
- [x] `-cl2` appears only when `SecondClient` is `true`
- [x] With no requirements or flags, the list is empty
- [x] With `GameClient = FiveMEnhanced`, the list is empty (no `-b`/`-pure_`/`-cl2`)
- [x] The list never includes a server address
- [x] The list is read-only
