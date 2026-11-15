# Review hardening: multi-session plan

Source: full project review of 2026-11-14 (security, bugs, performance, features).
Each session follows the repo workflow: `to-spec` -> `to-tickets` under `.scratch/<feature>/`,
TDD with the mandatory refactor step, `code-review`, one conventional commit per ticket.

## Execution

- Each session works in its own git worktree on its own branch; the user merges branches.
- Wave 1 (parallel, disjoint files): sessions 1, 2, 3.
- Wave 2 (sequential, all touch `MainViewModel` or `App.xaml.cs`): sessions 4, 5, 6.
- Wave 3 (sequential features): sessions 7, 8, 9.
- A wave starts only after the previous wave's branches are merged into `main`.

## Decisions already taken

- Icon hosts: measure the hosts in the live catalog first; allowlist the CFX metadata host
  only if effectively all published icons use it. Otherwise keep any-https and report.
- `cfxlauncher://`: opt-in Settings checkbox registers/unregisters under HKCU; when enabled,
  the registered path is refreshed at startup if the exe moved.
- Tray: icon always present with quick-connect menu; opt-in Settings checkbox makes minimize
  hide the window to the tray. Close still exits.
- Out of scope: log file, cancel button for the connect wait, browser locale/tag filters,
  server detail panel, history/favourites, import/export.
- No log file means the global exception handler only sets a status line.

## Wave 1

### 1. Icon pipeline hardening
Files: `Service/ServerEnrichmentService`, `Service/IconImageNormalizer`, `Service/BoundedContent`,
`Views/BytesToImageSourceConverter`.
- Non-image bytes behind `icon_url` must never reach WPF: validate decodability at download
  (undecodable -> null); the converter returns null instead of throwing. Reproduced: HTML
  bytes make the `Image.Source` binding throw `NotSupportedException`.
- Make size bounds real: `HttpCompletionOption.ResponseHeadersRead` for icon and catalog
  requests; dispose every `HttpResponseMessage` (catalog, icon, `CfxService`).
- Negative-cache failed icons so a failing URL is not re-downloaded every minute.
- Icon host policy per the decision above.
Done: garbage/HTML/corrupt-PNG icon bytes yield no icon and no exception anywhere; an
oversized body is abandoned at the cap; tests cover each.

### 2. Update and IPC hardening
Files: `Launch/UpdateApplier`, `Service/GitHubReleaseFeed`, `Service/LauncherUpdate`,
`Service/ConnectRequestListener`, `Service/ConnectRequestForwarder`.
- Verify the staged download against the asset's published sha256 `digest`; refuse when the
  digest is absent or mismatched (decide absent-digest behaviour in the spec).
- Accept only https GitHub download URLs; sweep stale `*.new-*` files with the `*.old-*` sweep.
- Pipe: per-session name, `PipeOptions.CurrentUserOnly`, read timeout so a silent client
  cannot block the listener, bounded line length.
Done: a tampered or truncated asset is never installed; a second Windows session gets its own
working pipe; a client that never writes does not stall later connects.

### 3. CI and test hygiene
Files: `.github/workflows/`, `tests/.../ViewModels/MainViewModelTests.cs`.
- Test-only GitHub Actions workflow on push/PR (windows-latest, .NET 10, build + test). No
  release automation (declined earlier).
- Fix flaky `RunEnrichmentLoopAsync_WhenRefreshThrows_ShouldKeepLooping` (failed 2 of 6 runs):
  replace the `Task.Yield` spin budget with a signal from the fake.
- Fix the xUnit1031 warning in `ConnectRequestListenerTests`.
Done: workflow green on the branch; the flaky test passes 50 consecutive runs.

## Wave 2

### 4. Persistence safety
Files: `Configuration/FileServerRepository`, `Configuration/FileSettingsStorage`,
`App.xaml.cs`, `ViewModels/RelayCommand`, repository call sites in `MainViewModel`.
- A failed or corrupt read must never be followed by a write that drops data: distinguish
  missing from unreadable, refuse mutation after an unreadable read, keep a `.bak` of a
  corrupt file, do not drop individually invalid entries on rewrite.
- Hold the saved list in memory (also removes the per-call file re-read).
- `DispatcherUnhandledException` handler (status line only) and guarded sync command paths:
  delete, reorder, drag, browser save must survive `IOException`/`UnauthorizedAccessException`.
Done: tests prove a locked/corrupt file is intact after Add/Update/Move/Remove attempts; a
read-only data directory produces a status message, not a crash.

### 5. Connect correctness
Files: `Domain/ServerResolver`, `Domain/ServerAddress`, `Service/CfxService`,
`Service/CitizenFxConfigWriter`, `MainViewModel.ConnectAsync`.
- CFX outage (5xx/timeout, not 404) on an id-form address -> unvalidated join-form connect
  instead of "Invalid address".
- Catalog-route profiles carry `DefaultBuild`, `ReplaceExecutable`, `PoolSizesIncrease` like
  the `/single/` route, so priming does not depend on the address form.
- `Classify`: the `cfx.re/join/` prefix must be anchored (optional scheme only before it).
- Trim the typed/pasted address before resolving.
- `CitizenFX.ini`: first confirm which encoding FiveM writes; preserve the file's encoding on
  rewrite so non-ASCII paths survive. Unverified suspicion - investigate before changing.
Done: each item pinned by tests; the ini item ends with either a fix or a written finding
that the current behaviour is safe.

### 6. Performance and small items
Files: `Service/ServerCatalog`, `Service/ServerCatalogDecoder`, enrichment loop in
`MainViewModel`, `ViewModels/AsyncRelayCommand`.
- Do not re-download the full catalog every 5 minutes while idle (pause when the window is
  minimized/hidden, or a lighter per-server path when the browser is closed - decide in spec).
- Decode catalog frames from the stream instead of holding the payload twice.
- Catalog User-Agent uses `AppInfo` instead of `FiveMServerLauncher/0.1`.
- A swallowed command exception must not leave the status at "Resolving...".
Done: tests pin the idle behaviour and the status reset; no behaviour change for the browser.

## Wave 3

### 7. Browser sorting
Sort the server browser by players and by name (asc/desc), composed with existing filters;
labels in all 11 languages.

### 8. `cfxlauncher://` URI scheme
`cfxlauncher://connect/<address>` parsed into the existing `--connect` path (first instance
and forwarded-to-running-instance); opt-in registration per the decision above; the address
goes through the normal validation pipeline, nothing new is trusted.

### 9. Tray icon
Tray icon with saved-server quick-connect, show window, exit; opt-in minimize-to-tray per the
decision above; quick-connect follows the forwarded-connect rules (ignored while busy).
