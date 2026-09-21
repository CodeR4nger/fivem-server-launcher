Status: ready-for-agent
Type: spec

# Prime the Legacy FiveM client's CitizenFX.ini from server-published facts

## Problem Statement

When a FiveM server enforces a specific game executable (`sv_defaultGameBuild`, or the legacy `sv_replaceExeToSwitchBuilds` mechanism) or raises pool size limits (`sv_poolSizesIncrease`), the game client compares those server facts against its own local state at connect time. When they differ, the client restarts itself in the middle of the join, or an incompatible executable tries to boot. The player lands back at the loading screen instead of in the server. Nothing before launch prepares the client, so the restart/reject happens on every join until the client has self-corrected.

## Solution

Before handing the connect URI to the process launcher, the launcher writes the server-published requirements into the Legacy FiveM client's `CitizenFX.ini` (`%localappdata%\FiveM\FiveM.app\CitizenFX.ini`, `[Game]` section) so the client boots already in the state the server requires — no restart-on-connect, no executable surprise.

The `.ini` keys and their sources, verified against the FiveM source tree (`code/client/shared/XBRInit.cpp`, `code/client/shared/CrossBuildRuntime.h`, `code/client/launcher/CrossBuildLaunch.cpp`, `code/components/citizen-server-impl/src/InitConnectMethod.cpp`):

- `[Game] DefaultBuild = <int>` — the persisted/mandated executable build. Client semantics: `GetGameBuild() = max(requested, persistedDefault)`; a `DefaultBuild > 0` wins over the legacy `ReplaceExecutable` path (`0` counts as absent, mirroring `GetDefaultBuildInit`).
  - Value source 1 (modern, wins): server var `sv_defaultGameBuild`.
  - Value source 2 (legacy fallback): when `sv_replaceExeToSwitchBuilds` is `true`, use `sv_enforceGameBuild`.
- `[Game] PoolSizesIncrease = <json>` — the server's `sv_poolSizesIncrease` value verbatim (e.g. `{"CWeaponComponentInfo":500}`), passed through unchanged. Writing it before launch prevents the client's restart-on-mismatch.

`SavedBuildNumber` is explicitly **not** written: the launcher's connect URI already carries `-b<build>` (from `sv_enforceGameBuild` via `FiveMLaunchOptions.ToUri()`), and `CrossBuildLaunch.cpp` only uses `SavedBuildNumber` to synthesize a `-b` flag when none is present.

The write is best-effort and gated: it only happens for CFX-validated Legacy FiveM profiles that publish at least one of the relevant facts, only when the current `.ini` value differs, and never changes any other key or section. A failed or skipped write never fails the connect — the client falls back to its built-in self-correction.

## User Stories

1. As a player, I want to join a server that enforces an executable build, so that my client boots with the right executable and I don't land back at a loading screen.
2. As a player, I want to join a server that raises pool size limits, so that the game doesn't restart mid-join because my local pool sizes differ from the server's.
3. As a player, I want my existing `CitizenFX.ini` customizations preserved, so that tuning or other legitimate keys are never lost when the launcher primes it.
4. As a player, I want the launcher to work even when the `.ini` write fails (read-only file, locked file, missing install), so that a config hiccup never blocks connecting.
5. As a player, I want nothing written when I open an Enhanced (legacy-inapplicable) client or connect to a server with no such published requirements, so that my config is left alone when priming has nothing to do.
6. As a developer, I want the launcher to prefer the server's explicit `sv_defaultGameBuild` and only fall back to `sv_replaceExeToSwitchBuilds`/`sv_enforceGameBuild`, so the decision mirrors FiveM's own precedence in `CrossBuildRuntime.h`.
7. As a developer, I want the `.ini` read-modify-write tested against the real file layout with temp files, so the round-trip is pinned (create, update, preserve-other-keys, no-op-when-unchanged).
8. As a developer, I want the "what to write and when" logic tested at a seam without any real filesystem, so decisions are fast and deterministic.
9. As a developer, I want the connect flow to remain best-effort around priming, so the `LaunchResult` contract (`Connect`/`OpenClient`/`StartFailed`) stays exact and unsalted.
10. As a developer, I want the new CFX facts (`sv_defaultGameBuild`, `sv_replaceExeToSwitchBuilds`, `sv_poolSizesIncrease`) carried through the existing facts chain, so the domain classes keep CFX-agnostic names per ADR 0001.
11. As a developer, I want `SavedBuildNumber` left out, so we never pin a build flag that the connect URI already provides and that other launch paths could inherit stale.

## Implementation Decisions

- **Fact plumbing (existing chain only, no new seams).**
  - `Service/CfxVars` gains `TryGetString` (missing key or null → null) and a strict boolean mapper `TryGetBool` accepting only `"true"`/`"false"` (anything else → null, mirroring `MapSteamTicket` strictness).
  - `Service/CfxService` reads three new vars into `CfxServerInfo`: `sv_defaultGameBuild` (int via `TryGetInt`), `sv_replaceExeToSwitchBuilds` (bool via `TryGetBool`), `sv_poolSizesIncrease` (raw string via `TryGetString`). All three are `ConVar_ServerInfo` on the server and appear in the `/single/` response `vars` like the already-mapped `sv_enforceGameBuild`/`sv_pureLevel`.
  - `Domain/ServerRequirements` gains three CFX-agnostic nullable fields, populated by `Domain/ServerRequirementsResolver` from `CfxServerInfo`: `DefaultBuild` (int?, from `sv_defaultGameBuild`), `ReplaceExecutable` (bool?, from `sv_replaceExeToSwitchBuilds`), `PoolSizesIncrease` (string?, the raw JSON from `sv_poolSizesIncrease`). No domain class names a CFX var.
- **Extend existing seam** `Service/IClientInstallLocator` with `Task<string?> GetInstallDirectoryAsync(GameClient)` — returns the install directory only when the client is installed, mirroring `GetExecutablePathAsync`. Real impl derives it from the paths it already owns. No new install knowledge anywhere else.
- **New seam `Service/ICitizenFxConfigWriter`** (+ real `Service/CitizenFxConfigWriter`): `Task ApplyAsync(string iniPath, IReadOnlyDictionary<string,string> values)` writes the given key→value pairs under `[Game]` via read-modify-write — creates the file (and its directory) with `[Game]` section if absent, preserves every other section/key/line content unchanged, rewrites only when a target value actually changes. Never touches keys not in `values`.
- **New seam `Launch/ICitizenFxPreparer`** (+ real `Launch/CitizenFxPreparer`): `Task PrimeAsync(ServerProfile profile)`. Sole owner of the decision:
  - Gate: only proceed when `profile.IsCfxValidated` and `profile.GameClient` is Legacy FiveM (Enhanced/RedM/null have no applicable `.ini`).
  - Build target values: `DefaultBuild` ← `Requirements.DefaultBuild` when `> 0`; else if `Requirements.ReplaceExecutable == true` and `Requirements.GameBuild` has a value ← `Requirements.GameBuild`; `PoolSizesIncrease` ← `Requirements.PoolSizesIncrease` when non-null and non-blank (whitespace-only counts as absent).
  - Resolve the `.ini` path via `IClientInstallLocator.GetInstallDirectoryAsync` for Legacy FiveM; nothing to do when not installed.
  - Swallow any exception (missing dir, IO error) — priming is best-effort and must never throw out of the connect flow.
- **Wiring**: `Launch/GameLauncher.ConnectAsync` awaits `ICitizenFxPreparer.PrimeAsync(profile)` as a pre-launch step before building `FiveMLaunchOptions`, swallowing any preparer exception so the connect flow never fails from priming; the existing options → URI → process-start flow and the `LaunchResult` hierarchy are untouched. Composition root wires `CitizenFxPreparer(new ClientInstallLocator(), new CitizenFxConfigWriter())` into `GameLauncher`.
- **FiveMLaunchOptions is unchanged**: the connect URI already carries `-b<gamebuild>`; with `DefaultBuild` primed, `GetGameBuild()` resolves to the server-mandated executable. `SavedBuildNumber` is deliberately not written (see Solution).
- AGENTS.md Launch/Service sections to be updated to mention the priming step and the new seams once implemented.

## Testing Decisions

- Good test = external behavior through the seam, per the repo's fake family (`FakeGameProcessLauncher`, `FakeUriSchemeRegistration`, `FakeDnsResolver`, `FakeHttpMessageHandler`, `FakeTimeProvider`): the code under test is the decision and the round-trip, not static calls.
- `GameLauncher` is tested through a `FakeCitizenFxPreparer` recording calls (prior art: `GameLauncherTests` + `FakeGameProcessLauncher`); a `ConnectAsync` test pins that priming runs before the process start and a second pins that a throwing preparer still yields the normal `LaunchResult` (the real preparer never throws, so the exception path is exercised through the fake).
- `CitizenFxPreparer` is tested with fake `IClientInstallLocator` + fake `ICitizenFxConfigWriter`: gates (unvalidated/Enhanced/RedM/none-installed/no-facts → no write), source precedence (`sv_defaultGameBuild` wins over legacy fallback), `sv_poolSizesIncrease` passed through verbatim, best-effort swallow of writer/locator failures.
- `CitizenFxConfigWriter` is tested against the real file layout with temp files/unique paths per test (prior art: `FileSettingsStorageTests` + `TempSettingsDirectory`): create-when-absent, update-only-when-changed (uphold the no-touch-unless-different contract), preserve all other keys/sections verbatim, never touch keys outside the target set.
- Fact plumbing: `CfxServiceTests` (via `FakeHttpMessageHandler`) pins the three new vars incl. invalid-value → null; `ServerRequirementsResolverTests` pins the mapping and CFX-agnostic field names; `CfxVars` mapping covered through those classes (`TryGetBool` strictness: only `"true"`/`"false"`).
- Naming `<Method>_Should<Expectation>` with Given/When/Then; full suite `dotnet test` green (153 total).
- Command: `dotnet test` (xUnit); one class: `dotnet test --filter FullyQualifiedName~CitizenFxPreparerTests`; build: `dotnet build FiveMServerLauncher.slnx`.

## Out of Scope

- Writing `SavedBuildNumber` (redundant with the `-b<build>` connect URI; see Solution).
- Any configuration for Enhanced or RedM (no `CitizenFX.ini` applies to those clients).
- Validating pool size limits or build numbers against CFX (the launcher passes server facts through verbatim; the server owns validation).
- Removing, reading back, or surfacing `.ini` values anywhere else (no settings UI, no diagnostics).
- Changing `FiveMLaunchOptions`, the `-b`/`-pure_` URI flags, or the `LaunchResult` contract.

## Further Notes

- Semantics verified in FiveM master source (not docs, which omit these keys): `XBRInit.cpp` (`GetDefaultBuildInit` reads `[Game] DefaultBuild`, `> 0` wins; legacy `[Game] ReplaceExecutable` backward-compat), `CrossBuildRuntime.h` (`GetGameBuild` = max of requested vs persisted default), `CrossBuildLaunch.cpp` (`SavedBuildNumber` only feeds a `-b` flag when none is present), `InitConnectMethod.cpp` (server vars `sv_defaultGameBuild`, `sv_replaceExeToSwitchBuilds`, `sv_poolSizesIncrease` are `ConVar_ServerInfo`, so they mirror to clients; the pool-size JSON is the `increase_pool_size` command's dump).
- The client restart behavior priming avoids is the client's own connect-time comparison (pool sizes compare against the local `GetIncreaseRequest()`; executable build against the persisted default).
- Git note: an earlier attempt to verify via GitHub commit `8a8c141` hit an unrelated commit; treat abbreviated SHAs from search results as untrustworthy.