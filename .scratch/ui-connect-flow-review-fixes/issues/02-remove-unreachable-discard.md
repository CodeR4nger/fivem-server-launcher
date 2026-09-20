# 02: Remove unreachable `_ =>` discard arm from LaunchResult switch

**What to build:** `MainViewModel.ConnectAsync` usa `switch` con `_ => Listo` al final; `LaunchResult` es jerarquía sealed (solo `Connect`/`OpenClient`); el discard nunca corre. Reemplazar por pattern-matching puro con los dos subtipos, sin el discard. Sin cambio de comportamiento observable.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `StatusText = result switch { LaunchResult.Connect => "Lanzando FiveM...", LaunchResult.OpenClient(var client) => $"Abriendo {client}..." };` sin `_ => Listo`.
- [x] Suite completa verde (109).

## Comments