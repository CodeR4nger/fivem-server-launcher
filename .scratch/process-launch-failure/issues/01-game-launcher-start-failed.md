# 01: GameLauncher returns StartFailed when the process fails

**What to build:** `IGameProcessLauncher.StartAsync` can throw exceptions (for example if `fivem://` is not registered). Today, those exceptions bubble up to the `GameLauncher`, break the async flow and never reach the VM. RED tests: fake with `StartAsync` throwing a `Win32Exception` → `LaunchResult.StartFailed` verified/marked. [] 

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `LaunchResult.StartFailed` (stateless sealed record).
- [x] `GameLauncher.ConnectAsync` attempts `await _processLauncher.StartAsync(uri)`, catches the exception and returns `LaunchResult.StartFailed`.
- [x] Fake throws-exception fixture in tests (`FakeGameProcessLauncher.ThrowOnStart`).
- [x] Suite green.

## Comments
