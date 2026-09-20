# 01: GameLauncher launches fivem://connect for a Legacy profile with CfxId

**What to build:** first vertical slice of the orchestrator. `GameLauncher.ConnectAsync(profile)` with a Legacy profile (CfxId + Requirements) delegates `fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1` to the `IGameProcessLauncher` seam and returns `LaunchResult.Connect(uri)`. Includes defining `LaunchResult` (immutable) and the `IGameProcessLauncher` seam + test fake.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `IGameProcessLauncher` with `StartAsync(Uri)`; test fake (`FakeGameProcessLauncher`) that records requests.
- [x] Immutable `LaunchResult` with payload; explicit factories (no public ctor).
- [x] `GameLauncher.ConnectAsync` with Legacy+CfxId+Requirements profile → seam receives `fivem://connect/cfx.re/join/y4lg95?-b3258?-pure_1`.
- [x] `Connect(uri)` result returned.
- [x] Full suite green.

## Comments

Note: the layer was named `Launch` (namespace `FiveMServerLauncher.Launch`) instead of `Application` because it collided with `System.Windows.Application` (WPF, `App.xaml.cs`).
