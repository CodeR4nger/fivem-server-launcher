Status: ready-for-agent
Type: spec

# ServerProfile — preparing requirements from CFX information

## Problem Statement

Today the launcher only resolves a CFX address to a `CfxId` (`ServerResolver.ResolveAsync`). DOC.md lays out the `ServerProfile → Requirements → FiveMLaunchOptions ↔ CFX server information` phase: when pointing at a server, the launcher must know what requirements that server publishes (Game Build, Pure Mode, Steam ticket) to prepare FiveM before connecting, **without manually duplicating** in configuration what the server already publishes and **without forcing** requirements on the user (the philosophy is "don't bother if everything is ready", but also don't assume anything the server doesn't state).

## Solution

When resolving an address, `ServerResolver.ResolveAsync` returns a `ServerProfile` that groups the server's identity and its `Requirements` derived **exclusively** from what CFX publishes (`sv_enforceGameBuild`, `sv_pureLevel`, `requestSteamTicket`). The user will be able to connect to a server confident that the launcher prepares what that server asks for, with no duplicated fields or manual override in this phase.

## User Stories

1. As a player, I want entering a CFX address to resolve the server and its state, so I can decide whether to join.
2. As a player, I want the launcher to determine the Game Build the server **enforces** (`sv_enforceGameBuild`), so FiveM launches with the correct build without me configuring it manually.
3. As a player, I want the launcher to determine the Pure Mode the server asks for (`sv_pureLevel`), so my session complies with the server's mode.
4. As a player, I want the launcher to detect whether the server requires a Steam ticket (`requestSteamTicket`), so I know whether Steam needs preparing before connecting.
5. As a player, I want the requirements to come from what the server publishes and not from my own manual copy, so the info doesn't go stale or get duplicated.
6. As a player, I want a requirement to stay absent/indeterminate when a server does **not** publish it (absent vars), so the launcher doesn't assume an arbitrary value.
7. As a player, I want the result of resolving an address to include the server name, so it can be shown in the UI later.
8. As a player, I want to keep the existing address-validation behavior (empty, not found), so resolution stays robust.
9. As a player, I want `cfx.re/join/<id>` (with or without scheme) to keep resolving the same, so the current input flow isn't broken.
10. As a developer, I want the requirements logic in the domain and not in UI/XAML, so I can test it without processes or filesystem.
11. As a developer, I want the CFX→requirements mapping to have a single source of truth, so parsing isn't duplicated across layers.
12. As a player, I want opening FiveM **directly** (Dev Mode, no server) to apply no server requirements, preserving the connect-vs-open separation.
13. As a player, I want the derived requirements not to mix with the launcher's global configuration (`LauncherSettings`), to keep `Configuration` to global preferences only.

## Implementation Decisions

- **`ServerResolver` is extended as the main seam** (confirmed with the user): `ResolveAsync` now returns a `ServerProfile` instead of `ResolvedServer`. Internally it composes `CfxService` + `ServerRequirementsResolver` (or the piece that derives requirements). Current address validation is preserved (empty → exception; not found → exception).
- **`ResolvedServer` is replaced/evolves into `ServerProfile`** in `Domain`: groups identity (`CfxId`, `ProjectName`) and `Requirements`. A single resolver output model.
- **`ServerRequirementsResolver` is extended** (building block, directly testable seam): `Resolve(CfxServerInfo)` produces all the requirements CFX publishes. Today only `GameBuild` existed; the other derived fields are added.
- **`ServerRequirements` covers only CFX-derived**: `GameBuild` ← `sv_enforceGameBuild`, `PureMode` ← `sv_pureLevel`, `RequestSteamTicket` ← `requestSteamTicket`. Each field stays nullable: absent = not published = indeterminate (avoids assuming arbitrary values).
- **`CfxServerInfo` already exposes** `EnforceGameBuild`, `PureLevel`, `RequestSteamTicket`; `CfxService` already parses them. `Service` doesn't change.
- **`LauncherSettings` is untouched**: still just `PreferredClient` + `AutoLaunch`.
- **No dev-mode in this phase**: opening FiveM directly doesn't use server requirements (DOC.md constraint); specified as user behavior but no UI or commands are added.
- Domain vocabulary per DOC.md: `ServerProfile`, `Requirements`, `FiveMLaunchOptions` (the latter is not built in this spec; only `Requirements`).

## Testing Decisions

- A good test here verifies **observable external behavior**, not implementation details: given a `CfxServerInfo` (or a fake HTTP response), the resulting profile exposes the expected requirements; when a var is absent, the requirement is `null`.
- **Modules to test:** `ServerResolver` (high-level seam: identity + composition + address validation) and `ServerRequirementsResolver` (var→requirement mapping, incl. the absent case).
- **Prior art in the repo:** `tests/.../Domain/ServerResolverTests.cs` (HTTP fakes via `FakeHttpMessageHandler`, Given/When/Then style, exceptions) and `tests/.../Domain/ServerRequirementsResolverTests.cs` (direct mapping). New tests follow that pattern, with no mocking library.
- The HTTP response is faked with `FakeHttpMessageHandler` and JSON fixtures as C# raw strings (repo convention).
- Naming: `<Method>_Should<Expectation>` with Given/When/Then comments.

## Out of Scope

- **IP:port / domain:port resolution** in `ServerResolver` (open TODO; requires research before assuming).
- **Manual `ServerProfile.Requirements` fields** (Steam/Discord as manual per-server requirements): another iteration.
- **`FiveMLaunchOptions`** (command-line arguments, CitizenFX.ini, `-cl2`, `fivem://connect`): later phase; requires researching the final application before modeling it.
- **UI/MVVM**: `MainView.xaml` stays static and unbound.
- **Dev Mode** and its build/pure overrides when opening FiveM directly.
- Steam/Discord as process detection or external-app preparation.

## Further Notes

- DOC.md:516-517: "If a technical decision depends on current FiveM/CFX information, research it before assuming." IP/domain resolution and launch options are exactly that case, which is why they are out.
- `GameClient` (FiveM | FiveMEnhanced | RedM) already travels in `CfxServerInfo`; it is not expanded in this spec but the `ServerProfile` must not lose it (it could be used in `Requirements` later).
- Repo ADRs in `docs/adr/` imported by `docs/agents/domain.md`; since there's no ADR for this area yet, this spec is the basis for a future ADR decision.
- After implementing, update `AGENTS.md` (current state) and commit with English conventional commit messages.
