# cfxlauncher:// web links

Session 8 of `.scratch/review-hardening/plan.md` (wave 3). Lets web pages and other apps open a
server in the launcher through `cfxlauncher://connect/<address>`, behind an opt-in Settings
checkbox; also removes a fixed sleep from the pipe listener tests.

## URI form and parsing (`Core/LauncherUri`, called from `Core/ConnectArgument`)

Windows hands the URI to the registered command as one argument (`"%1"`). `ConnectArgument.TryParse`
now yields the address of the LAST connect intent among `--connect <address>` and
`cfxlauncher:` arguments, so the URI rides the exact `--connect` paths (first instance →
`InitializeAsync(connectAddress)`, running instance → pipe → `HandleForwardedConnectAsync`).
A rejected URI is ignored like any unknown argument (an earlier intent still counts).

Decisions, all pinned by `LauncherUriTests`:
- Scheme and host are case-insensitive (`CFXLAUNCHER://Connect/...` works); the separator is
  exactly `://` and the host exactly `connect` followed by `/`.
- Anything that is not `connect/` (`cfxlauncher://open/x`, `cfxlauncher://connect`,
  `cfxlauncher:connect/x`, an empty address) → rejected: no connect, no crash.
- Query strings and fragments are IGNORED (cut at the first raw `?` or `#` before decoding):
  sites and link shorteners append tracking parameters, and no connectable address contains a
  raw `?`/`#`. An encoded `%3F`/`%23` survives into the address and the pipeline rejects it.
- Percent-decoding happens once (`Uri.UnescapeDataString`) after the cut; malformed sequences
  stay literal.
- Trailing `/` characters are stripped (browsers append one after the address).
- Transport hygiene, not validation: a decoded control character (e.g. `%0A`) rejects the URI —
  the forwarder speaks a one-line protocol, so a newline would truncate the address into a
  different one. Everything else is passed verbatim; `ServerAddress.Classify` (anchored join
  prefix, no whitespace, strict IP/domain rules) validates, exactly as for `--connect`.
- Shell safety: the address never reaches a shell command line raw — the launch URI is built
  from the classified address by `FiveMLaunchOptions`, and an unclassifiable address never
  launches. A raw `"` inside the URI could split the `"%1"` argument into extra args (e.g. a
  `--connect`), but that can only deliver another address into the same validated pipeline,
  which the URI could already do; browsers percent-encode `"` anyway.

## Registration (`Launch/LauncherSchemeRegistrar` over the extended `IUriSchemeRegistration`)

- Setting `LauncherSettings.WebLinks` (default false), persisted like `AutoLaunch`, Settings
  checkbox `WebLinksCheckbox` in all 11 languages.
- The registry is a seam: `IUriSchemeRegistration` (already the `fivem://` check) gains
  `GetUserOpenCommand(scheme)` / `RegisterForUser(scheme, displayName, exePath)` /
  `UnregisterForUser(scheme)`; the real `UriSchemeRegistration` writes
  `HKCU\Software\Classes\cfxlauncher` (default `URL:CFX Launcher`, `URL Protocol` = "",
  `DefaultIcon` = `"<exe>",0`, `shell\open\command` = `"<exe>" "%1"`) and is untested glue.
- Decisions (tested with a fake): enable writes when the key is free or ours; disable deletes
  only when ours; startup refresh (setting on) rewrites when the registered command differs
  from `"<Environment.ProcessPath>" "%1"` (moved exe, missing key), never over a foreign one.
- **Ownership rule**: a registration is ours when its open command's executable (first quoted
  token, or first whitespace token when unquoted) has the running exe's file name
  (case-insensitive, `CFXLauncher.exe`). No command → not a working registration, treated as
  free for enable, never deleted on disable. Foreign → enable refuses (status + revert),
  disable leaves it.
- Failure (registry `UnauthorizedAccessException`/`IOException`/`SecurityException`, null exe
  path, foreign owner): status `StatusWebLinksFailed`, the checkbox reverts, nothing persisted.
  The registry change runs first; if the settings save then fails, the registry change is undone
  best-effort and `StatusSaveFailed` shows. Startup refresh failures are silent.
- Two copies of the exe for one user: each has its own settings. Last copy to start (or toggle
  on) with the setting enabled owns the registration; disabling in any copy removes a CFX
  Launcher registration even if another copy wrote it — that copy re-registers on its next
  start. Accepted.
- Unwired registrar (null ctor param) = feature off: the setter only persists.

## Flaky listener test

`ConnectRequestListener.Completion` is the accept-loop task (completed when the loop exits);
`Start_WhenCancelled_ShouldStopAccepting` first proves the listener serves, cancels, awaits
`Completion` with a generous timeout, then asserts no server end. The fixed 150 ms sleep in the
handler-throws test was removed (the single server instance already orders the clients).
