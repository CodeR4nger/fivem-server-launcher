Status: resolved
Type: spec

# Post-review fixes for the game-launcher phase

## Problem Statement

The code review of the `game-launcher` phase (diff `8e7b60d...HEAD`, Standards and Spec axes) found two findings pointing at the same spot:

1. **Standards:** `LaunchResult` is a null-union: two nullable payloads (`ConnectUri`/`GameClient`) whose "kind" is inferred from which is non-null. An illegal state (both null or both set) is representable and the reader doesn't see the type's contract.
2. **Spec:** the spec asked for "`LaunchResult`: immutable type with `Kind` (`Connect | OpenClient`) and payload"; the implementation has no legitimate discriminator, only nullable payloads.

Additionally, the spec had an ambiguous line: "PreferredClient fallback from the profile" — the current fallback, `?? GameClient.FiveM`, does not consult `LauncherSettings.PreferredClient`. The current behavior is valid; it is documented as a decision: PreferredClient will arrive when the UI wires `LauncherSettings` (later phase).

## Solution

- Refactor `LaunchResult` into a sealed record hierarchy: `Connect(Uri) : LaunchResult` and `OpenClient(GameClient) : LaunchResult`. The discriminator is the runtime type (pattern matching); no separate `Kind` enum is needed. No public ctor on the base class. Static factories (or public records) allow creating instances but the two states are not confusable.
- The spec is clarified: "`GameClient` fallback is `FiveM` by default; `LauncherSettings.PreferredClient` will be read when the UI wires `ConfigurationRepository`" (later phase, out of scope here).
- The test `ConnectAsync_WithValidationCfxProfile` is renamed → `..._WithValidatedCfxProfile`.

## Testing Decisions

- TDD/adaptation: the `GameLauncher` tests change the assertion shape with the new hierarchy (pattern matching via `IsType`). No new behavior to validate — representation refactoring only.
- Full suite green (106) at the end.
