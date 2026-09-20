Status: resolved
Type: spec

# Fixes de post-review de la fase ui-connect-flow

## Problem Statement

El review de la fase `ui-connect-flow` (diff `852015f...HEAD`) encontró:

1. **Doc stale:** `AGENTS.md` dice "Future: `MainView.xaml` wires it..." pero el XAML ya está wired (binding hecho en el diff); la parte que queda es `DataContext`. Además la spec listaba `StatusColor` que no se implementó (YAGNI: texto solo) y no mencionaba la guarda `!string.IsNullOrWhiteSpace` en `CanConnect` (small creep OK).
2. **Unreachable arm en switch:** `StatusText = result switch { ..., _ => "Listo" }` — `LaunchResult` es jerarquía sealed (solo `Connect`/`OpenClient`), el `_` discard nunca corre. Queda como resurrection de la fase antigua.
3. **Cosméticos:** indentación rota en `MainViewModel.cs` (45-49) y en `[Fact]` de `MainViewModelTests.cs` (línea 16); fixture `CfxJson` usa raw string + `.Replace()` en vez de interpolación `$$"""` (standard del proyecto).

## Solution

- **Doc:** actualizar `AGENTS.md` (wired XAML, DataContext pendiente) + spec de la fase anterior no se toca (resuelta).
- **Code:** quitar `_ => "Listo"` del switch (sealed hierarchy cubierta).
- **Cosmetics:** re-dentar `MainViewModel.ConnectAsync` y `[Fact]` header del test; `CfxJson` → interpolación `$$"""`.
- La decisión "mantener guarda `ServerAddress` non-whitespace en `CanConnect`" se documenta en la spec de fase siguiente (o AGENTS.md actualización); no se ve como creep.

## Testing Decisions

- Suite verde (109). Sin tests nuevos (cleanup puro, la batería existente es la red).