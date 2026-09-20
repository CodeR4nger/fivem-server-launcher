# 01: LaunchResult with explicit discriminator (no null-union)

**What to build:** `LaunchResult` moves from a class with two nullable payloads to a sealed record hierarchy: `Connect(Uri) : LaunchResult` and `OpenClient(GameClient) : LaunchResult`. The discriminator is the type (pattern matching). It stays immutable, with no public ctor on the base; instantiation via final static factories. The `GameLauncher` tests are adapted to assert on the hierarchy (alongside the change, the malformed "ValidationCfx" test is renamed).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `LaunchResult` is a sealed hierarchy (`Connect(Uri)` / `OpenClient(GameClient)`); no public ctor, no null-union.
- [x] `GameLauncher.ConnectAsync` returns `new LaunchResult.Connect(uri)` or `new LaunchResult.OpenClient(profile.GameClient ?? GameClient.FiveM)`; compilation intact.
- [x] `GameLauncher` tests use pattern matching / `IsType` on the result (no generic assertions on a null-union).
- [x] `ConnectAsync_WithValidationCfxProfile_ShouldLaunchConnectUri` renamed → `WithValidatedCfxProfile`.
- [x] Full suite green (106).

## Comments
