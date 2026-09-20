# AGENTS.md

Windows-only C#/.NET 10 WPF launcher for FiveM. Design decisions and UX philosophy live in `DOC.md` (written in Spanish); commit messages are English conventional commits. Development is strictly TDD (RED -> minimal implementation -> GREEN -> REFACTOR), one layer at a time. The refactor step is mandatory: after every GREEN, improve the code — enforcing DRY, KISS, SOLID and YAGNI — with the suite still green, before starting the next test.

## Commands
- Build: `dotnet build FiveMServerLauncher.slnx`
- Test all: `dotnet test`
- Run one test class: `dotnet test --filter FullyQualifiedName~CfxServiceTests` (xUnit)
- Solution uses the new `.slnx` format: `dotnet sln FiveMServerLauncher.slnx add <project>`.
- Requires .NET 10 SDK. Verify with `dotnet --version` before running anything.

## Current state (verified; do not "fix" blindly)
- Build and all 63 tests pass (`dotnet test`).
- `Domain/ServerResolver.ResolveAsync` returns a `ServerProfile` (`CfxId`, `ProjectName`, `GameClient`, `Requirements`) composing `CfxService` + `ServerRequirementsResolver`. `ServerRequirements` carries CFX-published `GameBuild`, `PureMode`, `RequestSteamTicket` (all nullable when not published). `ServerResolver` extracts `cfx.re/join/<id>` from URLs via `ServerAddress.ExtractCfxId` (no validation — TODO validation). IP:port/domain resolution is still an open TODO.
- `Domain/FiveMLaunchOptions` (single seam, sealed class, immutable, private ctor — only the validated `Create`/`FromServerProfile` factories construct it; as a class it has no `with`, so the invariant is compiler-enforced) serializes launch intent: `Address` (`cfx.re/join/<id>`, null when opening directly), `GameClient`, `GameBuild`, `PureMode`, `SecondClient`. `ToUri()` returns `fivem://connect/<addr>?-b<build>?-pure_<nivel>` (null when no address or `FiveMEnhanced`); `ToCommandLineArgs()` returns an `ImmutableArray<string>` `-b<build> -pure_<nivel> [-cl2]` (empty for `FiveMEnhanced`, truly read-only). `Domain/ServerAddress` is the single owner of the `cfx.re/join/` form (extract id, derive address, validate form — no whitespace in id).
- `Service/CfxService.cs` uses the *requested* `cfxId` as `CfxServerInfo.CfxId` (the response `EndPoint` is a connection endpoint, not the id). `requestSteamTicket` distinguishes `on` → `true`, `off` → `false`, absent/invalid → `null`.
- UI is minimal and unbound: `Views/MainView.xaml` (static FiveM/server mock layout, one shared button `ControlTemplate`), `MainWindow` (custom window chrome). No commands/MVVM wiring yet. Business logic stays out of XAML code-behind; the single dropdown handler reads the label from `CommandParameter`.
- Branch `main` is ahead of `origin/main`; commits are conventional English per the workflow below.

## Architecture
- `Core/Enums`: shared types. `GameClient` = FiveM | FiveMEnhanced | RedM. Note `DOC.md` lists only two — the code is source of truth; `Service/CfxService.cs` maps CFX `gamename` (`gta5`, `gta5enhanced`, `rdr3`) to it.
- `Configuration/`: global launcher settings only. `LauncherSettings` is just `PreferredClient` + `AutoLaunch`. JSON persistence via `ISettingsStorage` -> `FileSettingsStorage` (System.Text.Json + `JsonStringEnumConverter`). Never add per-server fields, `Platform`, or `ServerPort` here.
- `Service/CfxService.cs`: queries `https://frontend.cfx-services.net/api/servers/single/{cfxId}`; typed vars include `sv_enforceGameBuild`, `sv_pureLevel`, `gamename`, `requestSteamTicket`.
- `Domain/ServerResolver.cs`: resolves a CFX id or `cfx.re/join/<id>` address. IP:port and domain resolution is an open TODO.
- `ConfigurationRepository` and `ServerRequirementsResolver` are intentional seams per `docs/adr/0001-intentional-middle-men-seams.md` (do not inline without reopening that ADR).
- `Domain/ServerAddress` owns the `cfx.re/join/<id>` form (single source for extract id, derive address, validate form), reused by `ServerResolver` and `FiveMLaunchOptions`.
- Keep business logic out of XAML code-behind and out of real processes/filesystem so tests stay fast.

## Tests
- xUnit, project targets `net10.0-windows` and references the main project. No mocking library — fake HTTP via `tests/.../Service/FakeHttpMessageHandler`, in-memory settings via `tests/.../Configuration/InMemorySettingsStorage`, and temp files via `tests/.../Configuration/TempSettingsDirectory` (`IDisposable`, unique path per test + teardown; all live in the test project).
- JSON fixtures use C# raw strings (`"""..."""`, `$$"""..."""`).
- Naming style: `<Method>_Should<Expectation>` with Given/When/Then comments.

## Workflow: Spec-Driven Development + TDD (agent skills)
- Skills live in `.agents/skills` (registered via `skills.paths` in `opencode.json`; `skills-lock.json` pins versions — update with `npx skills update`).
- Per feature: `to-spec` (or `spec-driven-development` from addyosmani for a full PRD) -> `to-tickets` -> `implement` (drives `/tdd`) -> `code-review`, then commit.
- `tdd` (mattpocock): RED->GREEN loop at pre-agreed seams only, one vertical slice per cycle. After each GREEN, run the mandatory REFACTOR step (DRY/KISS/SOLID/YAGNI) with the suite still green before starting the next test.
- .NET test helpers: `run-tests` before guessing a `dotnet test` command; `find-untested-sources`/`test-anti-patterns`/`test-gap-analysis`/`assertion-quality` for audits; `detect-static-dependencies` + `code-testing-agent` to add coverage.
- `domain-modeling`/`codebase-design` define module seam vocabulary; `diagnosing-bugs` for logic/CFX API bugs.

## Agent skills

### Issue tracker

Issues and specs are tracked as local markdown files under `.scratch/`. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout: one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.

## Hard design constraints (from DOC.md)
- RSC is never managed by the launcher. Steam/Discord are per-server `ServerProfile` requirements, never universal.
- Server-published (CFX) requirements win over manual config when connecting; Dev Mode build/pure overrides apply only when opening FiveM directly.
- Never equate "process started" with "service ready/authenticated".