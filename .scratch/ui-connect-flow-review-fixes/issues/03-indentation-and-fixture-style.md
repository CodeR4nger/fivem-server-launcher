# 03: Reformat indentaciones + interpolación en CfxJson

**What to build:** cosmética.
1. `MainViewModel.ConnectAsync` tiene indentación rota en el final del método.
2. `[Fact]` en `MainViewModelTests.ConnectAsync_WithValidCfxJoinAddress` incorrectamente alineado.
3. `CfxJson` usa `"""...Replace("VALUE", gamename)` — verificar si raw string `$$"""..."""` evita el error CS9007. Si no es posible devido al formato (CS9007 sobre `}}}`), documentar la decisión de mantener `.Replace`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Indentación en `MainViewModel.cs` correcta.
- [x] Indentación en `[Fact]` correcta.
- [x] `CfxJson` usa interpolated string (`$"...{{...}}..."`) con escape de braces dentro del raw literal — raw string `$"` y `$""""""` de `$"` con JSON causaban CS9007 por conflicto de `}}}`.
- [x] Suite completa verde (109).

## Comments