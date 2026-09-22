# RedM dev-mode + CitizenFX.ini support

**Status:** ready-for-agent

## Problem Statement

RedM is a first-class open target now, but two gaps remain per user:

1. **Dev Mode only launches Legacy FiveM** — RedM accepts the same CLI flags (`-b`, `-pure_`,
   `-cl2`), so dev users of RedM can't choose it.
2. **CitizenFX.ini priming is Legacy FiveM-only** — but RedM servers publish the same
   `sv_defaultGameBuild`/`sv_poolSizesIncrease` style vars, and RedM reads its own `CitizenFX.ini`
   under its install dir.

## Solution

- Dev panel gains a client selector/toggle (Legacy ↔ RedM). The dev launch applies the flags to
  the chosen client (both support them); Enhanced stays non-dev (plain open only).
- `CitizenFxPreparer` primes **both** Legacy and RedM CitizenFX.ini paths, with the same
  what/when rules (validated profiles only, pool sizes, default build, reset default pool for
  default-pool servers).

## User Stories

1. As a RedM dev player, I want Dev Mode to let me launch RedM with `-b`/`-pure_`/`-cl2`, so my
   dev flow matches Legacy.
2. As a Legacy dev player, I want to switch the dev client so my panel choice is reflected when I
   launch, without leaving Dev Mode.
3. As a RedM player connecting to a validated server, I want the server-published pool sizes and
   default build written into RedM's CitizenFX.ini just like Legacy does.
4. As a player, I want both dev targets to persist only build/pure (the `-cl2` remains one-shot).

## Implementation Decisions

- **Dev client selector:** a session-only `DevClient` toggle in the dev panel (Legacy/RedM);
  not persisted (per phase boundary: persisted dev state stays as build+pure only). The dev launch
  picks the client's exe through the existing `ClientInstallLocator` RedM support and stores no
  additional `LauncherSettings` field.
- **`CitizenFxPreparer`:** gating widens from `GameClient.FiveM` to `FiveM or RedM`; the ini path
  derives from the install locator's `GetInstallDirectoryAsync(client)`. Same write rules.
- No changes to `ToUri`/`ToCommandLineArgs`.

## Testing Decisions

- `CitizenFxPreparerTests`: RedM profile primes the RedM ini path with same rules; unvalidated
  RedM profiles prime nothing; Enhanced never.
- `MainViewModelTests`: clicking the RedM toggle persists nothing; launching picks the RedM exe
  path with the flags.
- Injection seam already exists on the locator.

## Out of Scope

- RedM dev persistence (client toggle lives for the session).
- Enhanced dev flags (unchanged).

## Further Notes

User decisions (asked): dev panel gets a **separate client toggle** (not from OPEN dropdown);
CitizenFX.ini priming uses **Legacy-equivalent rules** for RedM.
