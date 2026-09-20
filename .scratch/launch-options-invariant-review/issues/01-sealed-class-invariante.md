# 01: FiveMLaunchOptions from record to sealed class (compile-time invariant)

**What to build:** `src/FiveMServerLauncher/Domain/FiveMLaunchOptions.cs` moves from `sealed record` to `sealed class` with a private ctor and get-only props (no `init`), eliminating the `with` and blocking at compile time any invalid state outside `Create`. `ToUri()`/`ToCommandLineArgs()`/`IsEnhanced`/`BuildGameFlags`/`ValidateAddress`/`ValidateGameClient` are preserved without changing their behavior. `Create` and `FromServerProfile` remain the only public factories.

**Blocked by:** None

**Status:** resolved

- [x] `FiveMLaunchOptions` is a `sealed class` with private ctor and get-only props (no `init`), and no `with`/synthetic clone method exists — verifiable via reflection
- [x] There is no public setter of any kind (neither `init` nor `set`) on the props — reflection confirms it
- [x] `Create`/`FromServerProfile` remain the only public construction paths
- [x] `ToUri()` still returns `null` with null `Address` or `GameClient == FiveMEnhanced`, and URI/args with today's exact format (no behavior changes)
- [x] `Value_ShouldBeImmutableAfterConstruction` is adapted (no `with`): verifies absence of public mutators and non-identity of instances
- [x] Full suite green (61)
