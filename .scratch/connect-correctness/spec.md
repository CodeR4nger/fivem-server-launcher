# Connect correctness (review-hardening session 5)

Source: `.scratch/review-hardening/plan.md` section 5. Five findings from code reading; each
confirmed by a failing test before any change (or recorded here as a non-issue).

## 1. CFX outage on an id-form address

Today `ServerResolver.TryGetServerAsync` collapses a 404, a 5xx, a timeout and an unusable
200 body into `null`, and `ResolveCfxAsync` turns `null` into `InvalidAddressException`
("Invalid address") unless the saved server carries a direct address.

Contract after the change:
- `CfxService.GetServerAsync(id)` answers `null` ONLY for an authoritative "not found" (HTTP
  404). Every other failure is an outage and propagates in the sanctioned outage set
  (`HttpRequestException` / `IOException` / `OperationCanceledException`, i.e.
  `BoundedContent.IsOutage`): any non-success status other than 404 (5xx, 429, 403 edge
  blocks...), a transport failure, a timeout, and a 200 whose body is unusable (not JSON, or
  JSON without `Data`) — the last is reported as `HttpRequestException`, because a proxy or
  edge error page is not CFX saying the server does not exist. The body is read through
  `BoundedContent.GetBytesAsync` (4 MB cap; an over-cap body is an outage too).
  `BoundedBody` gains the response status so the 404 can be told apart.
- `ServerResolver` for `CfxId`/`CfxJoinUrl`:
  - found → validated profile (unchanged);
  - not found (404) → saved direct address if any, else `InvalidAddressException` (unchanged);
  - outage → saved direct address if any (precedence decision below), else a connectable
    UNVALIDATED join-form profile: `Address = cfx.re/join/<id>`, `IsCfxValidated = false`,
    no invented fields → `fivem://connect/cfx.re/join/<id>`; FiveM resolves the id itself.
- Precedence decision: the saved direct address wins over the join-form fallback on an
  outage. It is the user's own stored second path to the server, it does not depend on CFX
  at all, while the join form needs FiveM to reach the same CFX that is failing. This is
  also the existing behaviour, which only gains a final fallback instead of an error.
- Known limitation: the unvalidated join form has no `GameClient`, so the URI scheme is
  `fivem://` even for a RedM server joined by id during a CFX outage (the game is
  unknowable without CFX). The loopback carve-out is untouched: its manual id lookup still
  degrades to the manual values on any failure.
- Test rename: `Resolve_WhenCfxServiceOutageAndNoSavedDirectAddress_ShouldThrowInvalidAddress`
  and the two `CfxServiceTests` that claimed "no data / corrupt body → null" state claims
  this change makes false; they are deleted and rewritten under new names.

## 2. Priming parity between the catalog and `/single/` routes

`ServerResolver.BuildRequirements` (catalog route: IP:port, domain, bare forms) mapped only
`GameBuild`, `PureMode`, `RequestSteamTicket`, `SteamRequired`; the `/single/` route also
carries `DefaultBuild`, `ReplaceExecutable`, `PoolSizesIncrease`. `CitizenFxPreparer` resets
`PoolSizesIncrease` to empty when absent, so joining by IP:port cleared a pool increase the
same server publishes. Fix: one shared mapping `CfxVars.ToServerInfo(cfxId, projectName,
vars, requestSteamTicket)` builds the `CfxServerInfo` on both routes, and both routes go
through `ServerRequirementsResolver.Resolve` — the ADR-0001 seam is kept (it is now the
single CFX→Domain requirements translation for both routes instead of one of two).
`requestSteamTicket` keeps its per-route source: top-level field in `/single/`, the
`requestSteamTicket` var in the catalog (as before).

## 3. Anchored join prefix

`Classify` used `IndexOf("cfx.re/join/")` anywhere, so `x" --foo cfx.re/join/abc` was a
`CfxJoinUrl`, accepted by `SavedServer.Create`, and written unescaped into the desktop
shortcut's `--connect "<address>"`. Accepted join forms after the change (case-insensitive):
`[http://|https://][www.]cfx.re/join/<id>`, id = letters/digits/`-`/`_` (unchanged). The
same anchored parse backs `ExtractCfxId`. AGENTS.md promises "`cfx.re/join/<id>` with/without
scheme"; tests use `cfx.re/join/...` and `https://cfx.re/join/...` only; `www.` is added as
the form a browser address bar copies.

Stored-address compatibility: the repository hides (but preserves verbatim, re-emitted on
every write) entries whose address no longer classifies. Classes that stop classifying:
anything with text before the prefix other than the scheme/`www.` — e.g. a pasted
`fivem://connect/cfx.re/join/<id>` URI or `steam://...` wrappers, and injection strings.
The launcher itself never stores such forms (the dialog, browser save and id capture only
produce bare `cfx.re/join/<id>`), so only a hand-edited or pasted-URI row is affected; it
disappears from the list but is not deleted from disk. Accepted.

## 4. Pasted whitespace

`ConnectAsync` passed `ServerAddress` untrimmed to the resolver and the saved-server lookup;
`Classify` rejects any whitespace, so `"cfx.re/join/abc\n"` failed as "Invalid address".
Fix: `ConnectAsync` trims once at its boundary and uses the trimmed value for the saved
lookup, the resolve and `LastServerAddress`. `HandleForwardedConnectAsync` and the startup
`--connect` / auto-launch paths all funnel through `ConnectAsync` (they set `ServerAddress`
then await it), so the single trim covers them; pinned by a forwarded-connect test.

## 5. `CitizenFX.ini` encoding

Evidence (2026-11-15):
- citizenfx/fivem master: `code/client/launcher/GameSelect.cpp` (IVPath, lines ~151-439),
  `Updater.cpp` (UpdateChannel, ~775-821) and `Main.cpp` (~692) read/write CitizenFX.ini
  ONLY through `GetPrivateProfileString`/`WritePrivateProfileString`/`GetPrivateProfileInt`
  (wide variants, UNICODE build). Per the Win32 profile API, a file without a UTF-16LE BOM
  is read and written in the system ANSI code page (CP_ACP); FiveM never writes a BOM.
- Real file on this machine (`D:\My Games\FiveM\FiveM.app\CitizenFX.ini`, read only): 352
  bytes, no BOM, CRLF, 0 bytes ≥ 0x80 (the paths happen to be ASCII). System ACP = 1252.
- `IVPath` is the GTA V install path. A user path such as `C:\Juegos\José\GTA V` is
  written by FiveM as ANSI byte 0xE9, which is invalid UTF-8: `File.ReadAllLinesAsync`
  decodes it as U+FFFD and `AtomicFile.WriteAllText` writes `EF BF BD` back — the launcher
  corrupts IVPath on the first priming that changes a value, and FiveM loses the game path.

Real risk → fixed. The writer now preserves the bytes of every line it does not change:
- no BOM (FiveM's case): the file is processed as Latin-1 (a byte↔char bijection, so
  untouched lines round-trip byte-exact whatever code page wrote them), and the values the
  launcher writes are encoded in the ANSI code page (what FiveM's `GetPrivateProfileStringW`
  decodes with) — injectable for tests, default `Encoding.GetEncoding(0)` via
  `CodePagesEncodingProvider`;
- UTF-8 BOM: same byte-preserving path, BOM kept, written values encoded UTF-8;
- UTF-16LE BOM (the profile API's Unicode mode, e.g. a user-converted file): decoded and
  re-encoded as UTF-16LE with the BOM kept;
- a new file is created ANSI, as FiveM itself would create it.
