# Performance and small items (review-hardening session 6)

Source: `.scratch/review-hardening/plan.md` section 6. Five findings from code reading; each is
confirmed by a failing test (or a measurement) before any change.

## 1. Idle catalog re-downloads

Measured live on 2026-11-15: the streamRedir catalog is 20.7 MB (33,045 frames). The
per-minute enrichment loop calls `RefreshAsync` (TTL path), so while the window is open the
whole catalog is re-downloaded every time the 5 min TTL lapses — about 12 downloads / 248 MB
per hour — just to update a handful of saved rows. The dominant idle case is the user playing:
the game runs fullscreen and the launcher sits minimized or behind it, inactive.

Options weighed:
- **Per-server `/single/{id}` when the browser is closed.** Verified live: `/single/` carries
  `clients`, `sv_maxclients`, `vars.gamename` and `vars.icon_url`, so it could feed
  `ServerPresence`. But it also carries the full `players` array (60–90 KB for 700–1,000-player
  servers), it is one request per saved row per cycle, a 404 vs outage per row needs new
  semantics, and it adds a second presence data path next to the catalog (which the browser,
  the resolver and the cfx-id capture still need). Not chosen.
- **Pause while the window is away (chosen).** Smallest change: one data path, no new HTTP,
  and it removes the idle traffic exactly when nobody can see the rows.

Contract:
- "Away" = minimized OR not the active window. The composition root (`App.xaml.cs`, wiring
  only) feeds `MainViewModel.SetWindowAttended(bool)` from `Activated`/`Deactivated`/
  `StateChanged` (attended = `IsActive && WindowState != Minimized`). Default attended = true,
  so an unwired VM (every existing test) behaves as before.
- `RunEnrichmentLoopAsync` waits at the top of each cycle until the window is attended (the
  wait honours the cancellation token). A cycle never starts while away; when the window
  comes back after the loop parked, the cycle runs at once — the refresh-on-restore. A window
  that leaves and returns within one cadence wait causes no extra cycle.
- The paused cycle includes the CFX status refresh (same loop; small, but equally invisible).
- Unchanged: the manual ↻ (`RefreshServersCommand`, force path + 15 s cooldown), the
  post-save forced refresh, the browser (its own load/refresh; shares the catalog cache),
  connect-time catalog lookups.
- Trade-off accepted: a visible-but-inactive window (e.g. on a second monitor) shows rows as
  of the last cycle until it is activated again.
- Expected reduction: zero catalog downloads while away (previously ~12/h ≈ 248 MB/h); one
  download on return when the cache is older than the TTL.
- Unverified by tests: the real WPF `Activated`/`Deactivated`/`StateChanged` sequence (glue).

## 2. Catalog decode memory

`BoundedContent.ReadAsByteArrayAsync` grows a `MemoryStream` (capacity doubling) and
`ToArray()`s it, then `ServerCatalogDecoder.Decode(byte[])` parses it: the payload is held
twice (up to ~3x with the doubling slack) at peak, on top of the parsed objects.

Contract: the catalog decodes frames from the response stream as they arrive.
- `BoundedContent.ReadStreamAsync` shares the headers-read / status / declared-length cap /
  client-timeout / dispose rules with `GetBytesAsync` and hands the body stream to a reader.
- `ServerCatalogDecoder.DecodeAsync(Stream, long maxBytes, CancellationToken)` keeps the
  rules of `Decode(byte[])`: truncated trailing length prefix or frame → stop and return what
  decoded; corrupt frame → skip and continue; and the cap: more than `maxBytes` body bytes →
  `null` (outage, never decoded or cached). A claimed frame length is never pre-allocated:
  the frame buffer grows only as bytes actually arrive (a lying length cannot allocate).
- Peak live payload becomes one frame buffer (the largest frame) instead of the whole body.
- Measured with `GC.GetTotalAllocatedBytes` around a decode of the real 20.7 MB live
  payload; numbers recorded in the ticket.

## 3. Catalog User-Agent

`FiveMServerLauncher/0.1` → `CFXLauncher/<AppInfo.Version>`: one valid product token
(`AppInfo.ProductName` with its space removed; a token may not contain a space).

## 4. Stuck status after an unexpected failure

`AsyncRelayCommand.Execute` swallows everything; an unexpected exception inside
`ConnectAsync` (outside its `InvalidAddressException` catch) leaves `StatusText` at
"Resolving...". Contract:
- `ConnectAsync`, `OpenClientAsync`, `DevLaunchAsync` catch any other exception and end with
  the existing localized last-resort line (`MainViewModel.ReportUnexpectedError` →
  `StatusUnexpectedError`, "Something went wrong"); no new key. "Launch failed" was rejected:
  the failure may happen before anything launched (resolving, preparing apps).
  `IsBusy` is still reset by `finally`. The connect catch also covers the startup and
  forwarded connects, which call `ConnectAsync` directly.
- `AsyncRelayCommand` gains an optional `onError` callback, invoked inside its existing
  swallow; `MainViewModel` wires every one of its async commands to `ReportUnexpectedError`.
  No-crash behaviour unchanged: `Execute` still never throws, and a throwing callback is
  swallowed too. `ServerBrowserViewModel` keeps the plain swallow (it has no status line).

## 5. Test warnings

Clean rebuild showed 10 test-project warnings: xUnit1031 (blocking task ops) in
`ConnectRequestListenerTests`/`ConnectRequestForwarderTests`, CS8619 in
`FiveMLaunchOptionsTests`, CS8620 in the multi-value converter tests. Fixed in the tests
without weakening any assertion.
