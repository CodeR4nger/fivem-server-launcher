Status: resolved
Type: spec

# Post-review fixes for the ui-connect-flow phase

## Problem Statement

The review of the `ui-connect-flow` phase (diff `852015f...HEAD`) found:

1. **Stale doc:** `AGENTS.md` says "Future: `MainView.xaml` wires it..." but the XAML is already wired (binding done in the diff); the remaining part is `DataContext`. Also the spec listed `StatusColor` which was not implemented (YAGNI: text only) and did not mention the `!string.IsNullOrWhiteSpace` guard in `CanConnect` (small creep OK).
2. **Unreachable switch arm:** `StatusText = result switch { ..., _ => "Listo" }` — `LaunchResult` is a sealed hierarchy (only `Connect`/`OpenClient`), the `_` discard never runs. It survives as residue from the old phase.
3. **Cosmetics:** broken indentation in `MainViewModel.cs` (45-49) and in the `[Fact]` of `MainViewModelTests.cs` (line 16); the `CfxJson` fixture uses raw string + `.Replace()` instead of `$$"""` interpolation (project standard).

## Solution

- **Doc:** update `AGENTS.md` (wired XAML, pending DataContext) + the previous phase's spec is untouched (resolved).
- **Code:** remove `_ => "Listo"` from the switch (sealed hierarchy covered).
- **Cosmetics:** re-indent `MainViewModel.ConnectAsync` and the test's `[Fact]` header; `CfxJson` → `$$"""` interpolation.
- The decision "keep the `ServerAddress` non-whitespace guard in `CanConnect`" is documented in the next phase's spec (or an AGENTS.md update); it is not treated as creep.

## Testing Decisions

- Suite green (109). No new tests (pure cleanup, the existing battery is the safety net).
