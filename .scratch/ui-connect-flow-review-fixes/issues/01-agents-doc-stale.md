# 01: Stale doc in AGENTS.md (XAML already wired; DataContext pending)

**What to build:** `AGENTS.md` states "Future: MainView.xaml wires it with TextBox + Entrar al servidor + status TextBlock" but the diff `852015f...HEAD` already does that wiring. The part still pending is setting `DataContext = new MainViewModel(...)` in the composition root (later phase). Update the text.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] AGENTS.md describes the wiring as done (TextBox, command, statusText) and that the DataContext is pending the real composition (path: another phase) — not as "future MainView wiring".
- [x] Full suite green (no code change).

## Comments
