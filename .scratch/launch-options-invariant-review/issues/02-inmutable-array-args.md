# 02: ToCommandLineArgs returns ImmutableArray (truly immutable)

**What to build:** `src/FiveMServerLauncher/Domain/FiveMLaunchOptions.cs` changes `ToCommandLineArgs()` to return `ImmutableArray<string>` (add `using System.Collections.Immutable;`) instead of `string[]`, making mutation via index cast impossible. The signature changes from `IReadOnlyList<string>` to `ImmutableArray<string>`. Identical observable behavior: same args, `-b` before `-pure_` order, conditional `-cl2`, empty for `FiveMEnhanced`.

**Blocked by:** 01

**Status:** resolved

- [x] `ToCommandLineArgs()` signature is `ImmutableArray<string>`
- [x] The returned value is NOT a `string[]` (test by type/reflection), so `((IList<string>)args)[0] = "x"` cannot mutate the result returned to the caller
- [x] The prior RED test confirms the failure against the current `string[]` (index mutability) before the change
- [x] `ToCommandLineArgs_ShouldReturnReadOnlyList` is adapted to the new type (the value is immutable, contains the correct args, and is empty for Enhanced)
- [x] Correct args: `["-b3258", "-pure_1", "-cl2"]` with `SecondClient`, and empty for `FiveMEnhanced`
- [x] Full suite green (62)
