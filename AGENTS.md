# AGENTS.md

Windows-only C#/.NET 10 WPF launcher for FiveM. Design decisions and UX philosophy live in `DOC.md` (written in Spanish); commit messages are English conventional commits. Development is strictly TDD (RED -> minimal implementation -> GREEN -> REFACTOR), one layer at a time. The refactor step is mandatory: after every GREEN, improve the code — enforcing DRY, KISS, SOLID and YAGNI — with the suite still green, before starting the next test.

## Commands
- Build: `dotnet build FiveMServerLauncher.slnx`
- Test all: `dotnet test`
- Run one test class: `dotnet test --filter FullyQualifiedName~CfxServiceTests` (xUnit)
- Solution uses the new `.slnx` format: `dotnet sln FiveMServerLauncher.slnx add <project>`.
- Requires .NET 10 SDK. Verify with `dotnet --version` before running anything.

## Current state (verified; do not "fix" blindly)
- Build and all 110 tests pass (`dotnet test`).
- `Domain/ServerResolver.ResolveAsync` accepts the four DOC.md address forms (`CfxId`, `cfx.re/join/<id>` with/without scheme, `IP:port`, `domain:port`) via `ServerAddress.Classify` and returns a `ServerProfile` (`CfxId`, `Address`, `ProjectName`, `GameClient`, `Requirements`, `IsCfxValidated`). `ServerRequirements` carries CFX-published `GameBuild`, `PureMode`, `RequestSteamTicket` (all nullable when not published).
  - `CfxId`/`CfxJoinUrl` → `CfxService` (+`ServerRequirementsResolver`), `IsCfxValidated=true`; unknown form → `InvalidAddressException`.
  - `IpPort`/`DomainPort` → `ServerCatalog` lookup (streamRedir catalog). Found → validated profile built from catalog vars; not found/unavailable → connectable unvalidated profile (`IsCfxValidated=false`, `Address` = raw address, no invented fields). `domain:port` resolves DNS→IP for the match via `IDnsResolver` (seam); DNS failure or catalog outage also degrade to unvalidated (no exception).
- `Domain/FiveMLaunchOptions` (single seam, sealed class, immutable, private ctor — only the validated `Create`/`FromServerProfile` factories construct it; as a class it has no `with`, so the invariant is compiler-enforced) serializes launch intent: `Address` (any address classified as `CfxJoinUrl`/`IpPort`/`DomainPort`, or null when opening directly), `GameClient`, `GameBuild`, `PureMode`, `SecondClient`. `FromServerProfile` uses `CfxId` when set (join form) and falls back to the profile's raw `Address` for unvalidated servers. `ToUri()` returns `fivem://connect/<addr>?-b<build>?-pure_<nivel>` (null when no address or `FiveMEnhanced`); `ToCommandLineArgs()` returns an `ImmutableArray<string>` `-b<build> -pure_<nivel> [-cl2]` (empty for `FiveMEnhanced`, truly read-only). `Domain/ServerAddress` is the single owner of the `cfx.re/join/` form and of address classification (`ServerAddressKind`, `Classify`) + validation (IP octets/port 1-65535, domain labels/TLD-with-letter, no whitespace).
- `Service/CfxService.cs` uses the *requested* `cfxId` as `CfxServerInfo.CfxId` (the `/single/` response `EndPoint` is a connection endpoint, not the id — do NOT use it as a cfx id). `requestSteamTicket` distinguishes `on` → `true`, `off` → `false`, absent/invalid → `null`. Variable mapping is shared via `Service/CfxVars` (`TryGetGameClient`/`TryGetInt`/`MapSteamTicket`; gamename→`GameClient`, ints, steam ticket).
- `Service/ServerCatalog` (seam, `HttpClient` + injectable `TimeProvider`/TTL) downloads `https://frontend.cfx-services.net/api/servers/streamRedir/` (binary frames: uint32 LE length + protobuf `master.Server`), caches with ~5 min TTL, and answers `LookupByIpPortAsync` (matches `connectEndPoints`) / `LookupByEndPointAsync` (matches `EndPoint`). In catalog frames `EndPoint` IS the canonical cfx server id (so `ServerResolver.BuildValidatedProfile` may use it as `CfxId`) — unlike the `/single/` response, where `EndPoint` is a connection endpoint. Outage/corrupt frames → no match, never throws (only `HttpRequestException`/`TaskCanceledException` are swallowed as outages; other exceptions propagate). Protobuf codegen via `Google.Protobuf` + `Grpc.Tools` (`Proto/master.proto`, generated `Master.Server`/`ServerData`, no `Player`).
- UI is minimal and unbound: `Views/MainView.xaml` already wires the connect flow (TextBox `ServerAddress`, "Entrar al servidor" button `Command="{Binding ConnectCommand}"`, status `TextBlock` bound to `StatusText`) to `ViewModels/MainViewModel`; business logic stays out of XAML code-behind; the single dropdown handler reads the label from `CommandParameter`. What remains is assigning the `DataContext` (composition root, fase posterior).
- Branch `main` is ahead of `origin/main`; commits are conventional English per the workflow below.

## Architecture
- `Core/Enums`: shared types. `GameClient` = FiveM | FiveMEnhanced | RedM. Note `DOC.md` lists only two — the code is source of truth; `Service/CfxService.cs` maps CFX `gamename` (`gta5`, `gta5enhanced`, `rdr3`) to it.
- `Configuration/`: global launcher settings only. `LauncherSettings` is just `PreferredClient` + `AutoLaunch`. JSON persistence via `ISettingsStorage` -> `FileSettingsStorage` (System.Text.Json + `JsonStringEnumConverter`). Never add per-server fields, `Platform`, or `ServerPort` here.
- **Composition root**: `App.xaml.cs` `OnStartup` builds the object graph manually (`HttpClient` → `CfxService`/`ServerCatalog` → `ServerResolver` → `GameLauncher` → `MainViewModel`) and sets `MainWindow.DataContext`; no DI container, no `StartupUri` in `App.xaml`.
- `Launch/` (Application layer; named `Launch` to avoid clashing with `System.Windows.Application` in WPF): `GameLauncher.ConnectAsync(ServerProfile)` builds `FiveMLaunchOptions` and returns a `LaunchResult` — a sealed record hierarchy (`Connect(Uri)` after delegating the `fivem://connect/...` URI to `IGameProcessLauncher` the process seam, `OpenClient(GameClient)` for `FiveMEnhanced`, or `StartFailed` on launch exception; no null-union — the type IS the discriminator). Real process start is out of scope for now.
- `ViewModels/` (MVVM layer): `MainViewModel` (ServerAddress/IsBusy/StatusText/ConnectCommand) drives the connect flow; uses `ServerResolver` + `GameLauncher`, catches `InvalidAddressException` → "Dirección inválida". `AsyncRelayCommand` is a hand-rolled ICommand (no libraries). `MainView.xaml` already has TextBox + "Entrar al servidor" button + status TextBlock wired via `Binding`; business logic stays out of XAML code-behind.
- `Service/CfxService.cs`: queries `https://frontend.cfx-services.net/api/servers/single/{cfxId}`; typed vars include `sv_enforceGameBuild`, `sv_pureLevel`, `gamename`, `requestSteamTicket`.
- `Service/ServerCatalog.cs` (seam): downloads + caches (TTL) the streamRedir catalog (protobuf frames via `Service/ServerCatalogDecoder` + `Proto/master.proto` codegen); answers `LookupByIpPortAsync`/`LookupByEndPointAsync`. `LookupByEndPointAsync` (match by cfx id) is currently used only by tests but stays as part of the catalog seam contract, ready for future id-based lookups. Never throws on outage/corruption — returns no match. In catalog frames `EndPoint` is the canonical cfx id; in `/single/` responses it is a connection endpoint.
- `Service/CfxVars.cs`: shared variable mapping (`gamename`→`GameClient` via `TryGetGameClient`, ints via `TryGetInt`, `requestSteamTicket` via `MapSteamTicket`) used by `CfxService` and `ServerResolver`.
- `Domain/ServerResolver.cs`: resolves the four address forms; `IpPort`/`DomainPort` degrade to a connectable unvalidated profile when not published in CFX (or on DNS/catalog failure). `IDnsResolver` is the DNS seam.
- `ConfigurationRepository` and `ServerRequirementsResolver` are intentional seams per `docs/adr/0001-intentional-middle-men-seams.md` (do not inline without reopening that ADR).
- `Domain/ServerAddress` owns the `cfx.re/join/<id>` form and address classification/validation (`ServerAddressKind`, `Classify`, `ExtractCfxId`, `HasServerFormWithNonEmptyId`), reused by `ServerResolver` and `FiveMLaunchOptions`.
- Keep business logic out of XAML code-behind and out of real processes/filesystem so tests stay fast.

## Tests
- xUnit, project targets `net10.0-windows` and references the main project. No mocking library — fake HTTP via `tests/.../Service/FakeHttpMessageHandler`, fake time via `tests/.../Service/FakeTimeProvider`, fake DNS via `tests/.../Domain/FakeDnsResolver`, protobuf frame fixtures via `tests/.../Service/TestProtobufFrames`, in-memory settings via `tests/.../Configuration/InMemorySettingsStorage`, and temp files via `tests/.../Configuration/TempSettingsDirectory` (`IDisposable`, unique path per test + teardown; all live in the test project).
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