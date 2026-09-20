# 02: GameLauncher returns OpenClient for FiveMEnhanced without calling the seam

**What to build:** a profile with `GameClient = FiveMEnhanced` produces no URI (`ToUri()` → null). `GameLauncher.ConnectAsync` must return `LaunchResult.OpenClient(FiveMEnhanced)` **without** calling `IGameProcessLauncher` (it is the UI that opens the client afterwards). The miss in slice 01 is that `ConnectAsync` delegates without validating the Enhanced case.

**Blocked by:** 01

**Status:** resolved

- [x] `ConnectAsync` with an Enhanced profile → `LaunchResult.OpenClient(GameClient.FiveMEnhanced)`.
- [x] The fake records 0 seam calls in this case.
- [x] Full suite green.

## Comments
