# Feature: Game builds by name in a dropdown

Status: ready-for-agent

## Problem Statement

Entering a game build means typing a raw number (3095, 2802...) in Dev Mode and in the LOCAL DEV
panel. Users do not know build numbers offhand, do not know which builds are actually supported,
and can type numbers FiveM will silently ignore.

## Solution

Replace the build-number textboxes with a dropdown listing the supported builds with their
human names (for example `3095 — Latest`, `2802 — Los Santos Drug Wars`), curated per game client
(FiveM vs RedM). The selection persists as the same integer, so the saved-server schema and
launch options are unchanged. A research ticket first establishes the authoritative supported
build lists from the CFX documentation.

## User Stories

1. As a developer/server owner, I want to pick a game build from a named list instead of typing
   a number, so I no longer have to look build numbers up.
2. As a developer, I want the list to only contain builds FiveM/RedM actually support, so I can
   not configure a silently-ignored build.
3. As a developer, I want a `None (default)` option, so I can clear an override as today.
4. As a developer, I want the LOCAL DEV panel's build list to follow the GAME selection (FiveM
   builds for FiveM, RedM builds for RedM), so the choices are always relevant.
5. As a developer, I want the Dev Mode build dropdown to offer the FiveM list (its client toggle
   covers Legacy/RedM), so both surfaces work the same way.
6. As a developer, I want previously saved numeric builds (including ones no longer listed) to
   keep loading and displaying, so old data never breaks the UI.

## Implementation Decisions

- A curated static list in code (small domain type, e.g. `GameBuildOption(int? Build, string
  Label)` per game client); no runtime fetching of build lists.
- A research ticket (blocking the implementation ticket) produces the authoritative list from
  CFX's documentation of `sv_enforceGameBuild` for FiveM and RedM, with update names; the list
  is data the compiler checks (keyed constants), not free text.
- Persistence is unchanged: `DevGameBuild` in settings and `GameBuild` on saved servers remain
  `int?` (null = None). Unknown stored values render as their raw number entry.
- Enhanced ignores build flags entirely (existing launch semantics); with GAME = FiveM Enhanced
  selected the build combo disables.
- Localization: labels reuse the existing localizer seam; build names are proper nouns and are
  not translated; the `None` entry reuses the existing key.

## Testing Decisions

- Option-list construction is pure logic and fully tested: per-game lists, None entry, Enhanced
  disabling, unknown-stored-value display.
- Persistence round-trips stay covered by the existing settings/saved-server suites (no schema
  change expected).
- The research ticket's list is pinned by a test asserting the curated constants (so a bad edit
  fails loudly).

## Out of Scope

- Fetching supported builds from CFX at runtime; per-server automatic build detection; anything
  changing `FiveMLaunchOptions`/launch flag semantics.

## Further Notes

- Runs after the reorder feature per the agreed order; the dropdown inherits the custom control
  styling from the UI-polish feature.
