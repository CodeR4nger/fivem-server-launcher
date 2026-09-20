# 01: Doc-stale en AGENTS.md (XAML ya está wired; DataContext pendiente)

**What to build:** `AGENTS.md` afirma "Future: MainView.xaml wires it with TextBox + Entrar al servidor + status TextBlock" pero el diff `852015f...HEAD` ya hace ese wiring. La parte que sigue pendiente es setear `DataContext = new MainViewModel(...)` en el composition root (fase posterior). Actualizar el texto.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] AGENTS.md describe lo estar wired (TextBox, command, statusText) y que el DataContext está pendiente de la composition real (path: otra fase) — no como "future MainView wiring".
- [x] Suite completa verde (no cambia código).

## Comments