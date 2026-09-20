# 03: Reformat indentation + interpolation in CfxJson

**What to build:** cosmetics.
1. `MainViewModel.ConnectAsync` has broken indentation at the end of the method.
2. `[Fact]` in `MainViewModelTests.ConnectAsync_WithValidCfxJoinAddress` incorrectly aligned.
3. `CfxJson` uses `"""...Replace("VALUE", gamename)` — check whether a `$$"""..."""` raw string avoids the CS9007 error. If not possible due to the format (CS9007 on `}}}`), document the decision to keep `.Replace`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Indentation in `MainViewModel.cs` correct.
- [x] Indentation in `[Fact]` correct.
- [x] `CfxJson` uses an interpolated string (`$"...{{...}}..."`) with brace escaping inside the raw literal — raw string `$"` and `$""""""` with JSON caused CS9007 due to the `}}}` conflict.
- [x] Full suite green (109).

## Comments
