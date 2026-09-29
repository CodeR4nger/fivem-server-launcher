# 04: Close the composition-root phase

**What to build:** close-out. AGENTS.md -> real `ViewModels` layer wiring (App assigns `DataContext`); green suite; commit.

**Blocked by:** 03

**Status:** resolved

- [x] AGENTS.md updated (real MainWindow wiring, `composition` layer name, DataContext in `App`). Test count (109).
- [x] Spec `resolved`.
- [x] Full suite green, clean build.
- [x] English conventional commit (`9a904bf` feature, `a992f29` tickets).

## Comments

- 2026-09-28: tickets 01–04 statuses were stale — the phase-close commit `a992f29` added the ticket
  files with `ready-for-agent` and never flipped them. Re-verified against the code
  (`Service/DnsResolver.cs`, `Launch/GameProcessLauncher.cs` + `ProcessStarter`, `App.xaml.cs`
  manual graph with `window.DataContext = viewModel; window.Show()`, no `StartupUri`) and closed.
