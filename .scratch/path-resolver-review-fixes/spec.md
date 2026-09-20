Status: resolved
Type: spec

# Post-review fixes for the path-resolver phase

## Problem Statement
Review of `f00d3a7...HEAD`:
- **Spec**: `GetExecutablePathAsync` returned null only implicitly ("Should return null only when installed"); the real one didn't respect the specification (it returned a nonexistent path internally). There was also an unrequested RedM mapping (scope creep) and tautological fake tests.
- **Standards**: tests with "ViaRealImpl" name suffixes; fake tests folded a `LastQueriedClient` capture (dead); typo `Enhaced`.

## Solution
- `ClientInstallLocator` with injectable ctor `Func<string,bool> exists` (default: `File.Exists`) — repo injectable seam, not a different seam.
- `GetExecutablePathAsync` returns null when not installed; `GetPath` doesn't map RedM (scope creep removed).
- Tests updated: `ClientInstallLocator` with fake exists → real impl testable; consistent rename "null when not installed" without "ViaRealImpl" suffixes; dead `LastQueriedClient` removed from the fake.

## Testing Decisions
- Green suite (117 = previous 114 + 3 new RED/GREEN).
- Keep `AGENTS.md` updated (suite count).

## Out of Scope
- Steam/Discord detections (another phase), PathResolver enrichments (custom/etc.), YAGNI abstractions.
