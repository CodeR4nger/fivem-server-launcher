# Feature: Game builds by name in a dropdown

Status: ready-for-agent

## Problem Statement

Entering a game build means typing a raw number (3095, 2802...) in Dev Mode and in the LOCAL DEV
panel. Users do not know build numbers offhand, do not know which builds are actually supported,
and can type numbers FiveM will silently ignore.

## Solution

Replace the build-number textboxes with a dropdown listing the supported builds with their
human names (for example `[3095] The Chop Shop`, `[2802] Los Santos Drug Wars`; RedM builds
and unnamed builds show just the number), per game client (FiveM vs RedM). The selection persists as the same integer, so the saved-server schema and
launch options are unchanged. The build list is a curated baseline in code that also
**auto-updates once per launcher session** from CFX's own sources (numbers from the FiveM
build-system file, names from the CFX frontend bundle the client itself renders its "DLC:"
pill from), so a new GTA DLC appears with no launcher release. A research ticket established
the authoritative sources and today's baseline data.

## User Stories

1. As a developer/server owner, I want to pick a game build from a named list instead of typing
   a number, so I no longer have to look build numbers up.
2. As a developer, I want the list to only contain builds FiveM/RedM actually support, so I can
   not configure a silently-ignored build.
3. As a developer, I want a `None (default)` option, so I can clear an override as today.
4. As a developer, I want the LOCAL DEV panel's build list to follow the GAME selection (FiveM
   builds for FiveM, RedM builds for RedM), so the choices are always relevant.
5. As a developer, I want the Dev Mode build dropdown to follow the CLIENT toggle (FiveM builds
   on Legacy, RedM builds on RedM), so both surfaces work the same way. *(Amended at
   ticketing time from "offers the FiveM list" — a strictly-FiveM list would leave a
   RedM-toggled session without valid RedM builds; see ticket 04.)*
6. As a developer, I want previously saved numeric builds (including ones no longer listed) to
   keep loading and displaying, so old data never breaks the UI.

## Implementation Decisions

- A curated baseline in code (small domain type, e.g. `GameBuildOption(int? Build, string
  Label)` per game client): today's numbers and names, so the dropdown is complete offline.
- Auto-update, **once per session** (user decision): a small service fetches two CFX sources and
  merges them into the same dataset shape —
  - **numbers**: `code/premake5_builds.lua` on the citizenfx/fivem `master` raw URL — the
    FiveM build-system source of truth for supported builds (FiveM + RedM sections parsed; the
    GTA6-era `ny` section is ignored);
  - **names**: the `getGameBuildDLCName` switch mined from the CFX servers frontend JS bundle
    (`servers.fivem.net` — the same cfxui bundle the FiveM client embeds; its content-hashed
    file name is resolved from the page HTML first).
  - Any outage or format drift degrades to the curated baseline — never throws, never blocks.
- Names mirror the client's own mapping where it exists (the frontend bundle maps `2612` →
  "The Contract" and patch builds `3323`/`3788` → their parent DLC); the docs fill the legacy
  entries the bundle lacks (`1604` — Arena War, `1` — base game). Builds no source names
  (all RedM builds) render as their bare number.
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

- Per-server automatic build detection; anything changing `FiveMLaunchOptions`/launch flag
  semantics; runtime fetching beyond the two CFX sources named above (no third-party
  build-list APIs).

## Further Notes

- Runs after the reorder feature per the agreed order; the dropdown inherits the custom control
  styling from the UI-polish feature.
