# Feature: Server icons via icon_url + WebP (CFX icon pipeline migration)

Status: done

## Problem Statement

CFX changed how server icons are published (observed live 2026-10-26). The streamRedir
catalog no longer publishes the typed `iconVersion` proto field — 0 of 33,548 live
servers carry it, and the old download endpoint
`frontend.cfx-services.net/api/servers/icon/<id>/<version>.png` returns 404. The new
source is the `icon_url` var (25,697/33,548 servers = 77%, always
`https://cfx-server-metadata.cfx-services.net/images/<guid>`), present in both the
catalog vars and `/single/` responses. The new endpoint serves **WebP** — 25/25
sampled icons are `RIFF….WEBP`, canvases mostly 96×96, file sizes 1.6 KB to 5.07 MB —
and WPF/WIC cannot decode WebP natively. Net effect: every server icon in the
saved-server list and the browser is gone, and even a plain URL switch would render
nothing because the bytes are no longer PNG.

## Solution

Migrate the icon pipeline to `icon_url` end to end and normalize the new format at the
download boundary:

1. Read `icon_url` from catalog vars (`CfxVars.TryGetIconUrl`); carry it on
   `ServerPresence` and `ServerBrowserItem` as the single icon fact.
2. `IServerEnrichmentService.GetIconAsync(iconUrl)` downloads the URL directly
   (URL-keyed cache/single-flight/concurrency — mechanics unchanged), validates the URL
   before any network call, and bounds the download at a raised cap.
3. A vendored official `libwebp.dll` (x64, ~400 KB) + a thin P/Invoke seam decodes
   WebP after a header-dimension bomb gate; WPF resizes to ≤96 px and re-encodes PNG.
   The cache stores normalized PNGs, so every existing downstream consumer —
   `BytesToImageSourceConverter`, `BoundedIconDecode`, `ShortcutIconWriter` — keeps
   working byte-format-agnostic and the LRU stores ~KB PNGs instead of multi-MB raw
   files.
4. The dead `iconVersion` path is deleted, not dual-run (0 publishers, dead endpoint).

## User Stories

1. As a player, saved-server rows show their server's icon again.
2. As a player, browser rows show icons again (lazy-loaded as before).
3. As a player, desktop quick-connect shortcuts carry the server's icon when the row
   has one (the row bytes are what the ICO writer consumes).
4. As a player with a server that publishes no `icon_url`, the row stays iconless —
   same as before, never a broken image.
5. As a player, an icon that fails to fetch or decode (404, oversized canvas,
   undecodable bytes) degrades to no icon — never a crash, never a stalled row, never
   a failed refresh cycle.
6. As a player, hostile icon bytes can never blow up launcher memory
   (decompression-bomb protection continues on the new format).
7. As a player behind a firewall or during a CFX outage, behavior is unchanged: no
   icons, no errors.
8. As a user, the launcher only downloads from validated absolute https URLs — a
   malicious server operator cannot point it at plain-HTTP, non-absolute,
   credentialed, or `file://` targets through the `icon_url` var. (Any validated
   https URL is accepted by design — host allowlisting was explicitly declined in
   the Out of Scope section, so https-to-any-host, LAN included, is in bounds; the
   defenses are validation, the size cap, decode-or-null and pool-thread
   execution.)

## Implementation Decisions

- **Source of truth**: `vars["icon_url"]` (raw string) via `CfxVars.TryGetIconUrl`,
  replacing `CfxVars.TryGetIconVersion`. The typed field 11 stays in `master.proto` as
  wire-format documentation (it is still field 11 on the wire) but is no longer
  consumed anywhere. This inverts ticket 10's finding
  (`.scratch/security-performance-review/issues/10-…`): then, a var-based lookup
  matched nothing and the typed field was the truth; now CFX moved the truth into the
  var. Both eras agree on the principle: follow where the data actually is.
- **Presence**: `ServerPresence.IconUrl` (nullable). `ServerPresence.Offline` unchanged.
  The old `IconVersion` member is deleted.
- **`GetIconAsync(string iconUrl)`** (single-argument): a URL is self-versioned (a new
  icon gets a new guid URL), so it is the complete cache key — `cfxId` adds nothing
  and is dropped from the key and the signature. LRU cache, single-flight registration
  and concurrency bound keep the existing mechanics with the URL as key.
- **URL validation** (before any network call, else null): non-empty, absolute, scheme
  exactly `https`, host non-empty, no userinfo. This is load-bearing beyond hygiene:
  `HttpClient.GetAsync("file://…")` throws `NotSupportedException`, which is outside
  the sanctioned swallow set (`HttpRequestException`/`TaskCanceledException`) and would
  fault every caller sharing the in-flight download. Invalid → null, zero requests.
- **Download cap**: `maxIconBytes` default 2 MB → 8 MB (observed real icons up to
  5.07 MB; the old 2 MB cap would silently drop them).
- **Normalization at download** (`IconImageNormalizer`, Service layer — WPF imaging is
  already a Service-layer citizen via `ShortcutIconWriter`):
  - WebP bytes (`RIFF` + `WEBP` magic) → header dims via `WebPGetInfo` → bomb gate
    (max axis ≤ 1024, area ≤ 1024×1024 — generous for an asset rendered at 22 px) →
    `WebPDecodeBGRA` (first frame of animated input) → `BitmapSource` → resize so max
    axis ≤ 96 preserving aspect → `PngBitmapEncoder`.
  - Non-WebP bytes pass through unchanged (WPF-native formats keep the existing
    path; `BoundedIconDecode` bounds them at render time).
  - Any decode failure or gate rejection → null icon. Normalized bytes enter the
    cache; the per-icon byte instances downstream logic relies on (converter LRU,
    `item.Icon != icon` reference compare) flow exactly as before.
- **Decoder**: the official Google libwebp 1.6.1 binary (x64 Windows, ~400 KB) delivered
  through the MIT-licensed `Imazen.WebP.NativeRuntime.win-x64` NuGet package — NOT a
  committed dll blob. Recorded rationale: the official libwebp Windows archive ships
  static libraries only (no DLL), this dev machine has no MSVC/CMake to build one, and
  the NuGet transport keeps the repo's plain-`dotnet` workflow (RID-native layout flows
  through standard `runtimes/win-x64/native` resolution into both dev output and the
  single-file publish's `IncludeNativeLibrariesForSelfExtract` mechanism; libwebp
  security updates ride package updates, no binary to re-vendor). The P/Invoke wrapper
  is ours: a thin `IWebpDecoder` seam over `WebPGetInfo`/`WebPDecodeBGRA`/`WebPFree`
  (real implementation untested per the process-starter/COM-glue convention; a
  real-fixture end-to-end test pins that the native libwebp actually decodes
  in-process). Test fixtures for edge cases are generated via libwebp's own simple
  encoder (`WebPEncodeRGBA`) — no binary blobs beyond one committed real icon.
- **Consumers switch to `IconUrl`**: `MainViewModel.RefreshServerInfoAsync` (gate +
  call), `ServerBrowserItem.FromServer` + `IconUrl` property,
  `ServerBrowserViewModel.LoadIconAsync`, `FakeServerEnrichmentService`
  (`LastRequestedIconUrl`). XAML is untouched — `Icon` bytes bindings are unchanged.
- **Rejected alternatives** (recorded so nobody re-litigates):
  - *ImageSharp* (any patched version ≥ 4.1.2 / 3.2.0) enforces a build-time license
    key (`sixlabors.lic`); free products must still apply for a community key and every
    Release build fails without it. The key-free versions carry Oct-2026 high-severity
    decode CVEs — unusable for untrusted server bytes.
  - *SkiaSharp* drags ~10–15 MB of natives and has an open bundling issue (#4165)
    where an ~80 MB `libSkiaSharp.pdb` rides into `deps.json`.
  - *WIC "WebP Image Extension"* is an optional Store component — hit-or-miss on
    Windows 10 and outside our control for bomb-bounding.

## Testing Decisions

- **Presence**: `RefreshAsync` exposes `icon_url` from vars on the presence; missing
  var → null. The iconVersion claim-tests are deleted and rewritten under new names
  (house rule: a test whose name claims something the change made false is deleted and
  rewritten, not edited green).
- **`GetIconAsync(iconUrl)`**: URL-keyed caching and re-download-on-URL-change,
  single-flight sharing, concurrency bound, validation matrix (http/relative/userinfo/
  garbage/empty → null with zero recorded requests), raised size cap behavior,
  download failure → null, unexpected fault still propagates to joined callers.
- **Normalizer**: fake `IWebpDecoder` for the logic (bomb-gate rejection, passthrough
  of non-WebP, resize/aspect call-through, failure → null); real-fixture end-to-end
  test decoding a committed real CFX WebP icon (captured 2026-10-26,
  `cfx-server-metadata…/7665c31f-…`, 2.8 KB) → asserts PNG bytes and bounded
  dimensions — pinning the vendored dll in-process. Oversize-canvas fixture generated
  via `WebPEncodeRGBA` at test time.
- **Consumers**: view-model tests rewritten for `IconUrl` (fake records the requested
  URL; presence-without-`icon_url` rows never request).
- **XAML/glue**: none — no view changes exist in this feature.

## Out of Scope

- Animated WebP beyond the first frame (static thumbnail; decode failure degrades to
  null, the same slot as "no icon").
- Persisting icon bytes to disk (still memory-only per session).
- Any `/single/` icon probing (the catalog snapshot remains the only icon source).
- Host allowlisting for `icon_url` (decided: any validated https URL — resilient to
  CFX moving hosts; the defenses are the validation, the size cap, decode-or-null and
  off-UI-thread execution).
- ImageSharp / SkiaSharp / WIC-extension decoding (rejected above).
- Version bump / release choreography (release process is manual and separate).

## Further Notes

- Live evidence, captured 2026-10-26 against the real endpoints: catalog 21.1 MB /
  33,548 servers, `iconVersion` present on 0, `icon_url` on 25,697 (all
  `https://cfx-server-metadata.cfx-services.net/images/<guid>`); old icon endpoint
  404; bogus guid 404 (clean degradation); 25/25 sampled icons WebP; canvases mostly
  96×96 VP8X (one 400×400 lossy VP8); sizes 1.6 KB–5.07 MB; `/single/` for `y4lg95`
  also carries `icon_url` in vars. The 219 KB @ 96×96 sample strongly suggests
  animated or lossless WebP is common in the wild.
- The old pipeline's failure was total and silent by construction: `TryGetIconVersion`
  answered null for every server, so no row ever attempted a download — no error
  surfaced anywhere. That is why this shipped as a user-visible "icons are gone"
  report rather than a crash.
- Suggested ticket order: 01 presence/var → 02 URL download+validation → 03 WebP
  normalization+vendored dll → 04 consumers switch → 05 delete the iconVersion path +
  docs. Each is a vertical slice leaving the suite green; tickets 01–03 keep the dead
  path compiling until 04 switches its consumers, and 05 removes it.
