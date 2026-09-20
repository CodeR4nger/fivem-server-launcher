# 06: AGENTS.md and effort close-out

**What to build:** Update `AGENTS.md` to the real state after this phase: `FiveMLaunchOptions` is a `sealed class` (private ctor, get-only props — compile-time invariant), `ToCommandLineArgs()` returns `ImmutableArray<string>`, reflect the style centralization in `MainView.xaml`/`MainWindow.xaml` (if considered worth mentioning), and the real count of green tests in `Current state`. Mark `[x]` on all ticket checkboxes of this effort verified against the code, and commit the phase with an English conventional commit.

**Blocked by:** 01, 02, 03, 04, 05

**Status:** resolved

- [x] `AGENTS.md` describes `FiveMLaunchOptions` as a non-`record` `sealed class` with private ctor and compile-time invariant
- [x] `AGENTS.md` reflects `ToCommandLineArgs(): ImmutableArray<string>` and the real test count (62)
- [x] All tickets of this effort have `[x]` boxes verified against the code
- [x] The phase is committed with an English conventional commit (`refactor(domain)` 5c4b77f, `chore(tickets)` 03c68d4)
- [x] The full suite stays green at the committed point (62)

## Comments
- Tickets 01-05 resolved via TDD/refactor: `sealed class` without `with`, `ImmutableArray<string>` in `ToCommandLineArgs`, `BasedOn` in MainView styles, style+resources in MainWindow, arrange helper in ServerResolverTests.
- Phase committed in 5c4b77f + 03c68d4. Suite 62 green at the committed point.
