# Final project review + final release build

**Status:** done (tickets 01–03 resolved; suite 422 green; code review clean; final Release build 0 errors; commit `efc96b5`)

## Problem Statement

All roadmap phases are done and committed (`6f997b6` latest). Before shipping the
final version, run a project-wide review audit (static source-to-test pairing,
standards/spec review) and close the genuine test gaps it surfaces, then build
the final Release configuration.

## Solution

1. **Audit** with `find-untested-sources` (Roslyn pairing) + review.
2. **Close the genuine direct-test gaps** found by the audit — the newest public
   logic class `CfxStatusItem` and the four older data-only value converters
   (`GameClientToBrushConverter`, `InverseBoolToVisibilityConverter`,
   `SelectionEqualityToVisibilityConverter`, `BytesToImageSourceConverter`).
   The last phase established the direct-converter-test precedent
   (`CfxStatusToBrushConverterTests`); the older converters have none. All are
   public and standalone-testable.
3. **Keep as-is** (deliberate, repo-precedented): real OS seams
   (`ProcessStarter`, `DnsResolver`, `UriSchemeRegistration`, `UriShellStarter`,
   `App.xaml.cs`, XAML code-behind) are faked by the seam pattern and never
   directly tested; internal infrastructure (`CfxVars`, `RelayCommand`,
   `AsyncRelayCommand`, `DevLaunchParams`) is exhaustively covered transitively
   and there is intentionally no `InternalsVisibleTo`.
4. **Final code review** (Standards + Spec) on the closing diff.
5. **Final release build**: `dotnet build FiveMServerLauncher.slnx -c Release`
   (0 errors) and document the produced artifacts in the roadmap.
6. Commit conventionally, mark roadmap phase done.

## User Stories

1. As a developer, I want every public, standalone-testable type to have direct
   tests in the final version, so the shipped binary's logic is pinned.
2. As a developer, I want the final Release build to compile with 0 errors, so
   the version we call final is the version that actually builds.
3. As a developer, I want the audit's accepted-exempt list documented, so a
   reviewer can see why the seam/entry-point files stay untested.

## Out of Scope

- Adding `InternalsVisibleTo` or testing internal helpers/seams directly.
- New product features, new UI, behavior changes.
- Auto-update (still an unticketed independent roadmap item).