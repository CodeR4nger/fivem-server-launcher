# 02: Remove unreachable `_ =>` discard arm from LaunchResult switch

**What to build:** `MainViewModel.ConnectAsync` uses a `switch` with `_ => Listo` at the end; `LaunchResult` is a sealed hierarchy (only `Connect`/`OpenClient`); the discard never runs. Replace it with pure pattern matching on the two subtypes, without the discard. No observable behavior change.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `StatusText = result switch { LaunchResult.Connect => "Lanzando FiveM...", LaunchResult.OpenClient(var client) => $"Abriendo {client}..." };` without `_ => Listo`.
- [x] Full suite green (109).

## Comments
