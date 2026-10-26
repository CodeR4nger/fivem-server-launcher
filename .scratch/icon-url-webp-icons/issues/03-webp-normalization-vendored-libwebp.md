# 03 — WebP normalization at download (vendored libwebp + P/Invoke seam)

Status: done

## What

Icon bytes fetched by the URL-keyed `GetIconAsync` are normalized to PNG before they
enter the cache:

- New `IWebpDecoder` seam (Service): decode WebP bytes → raw BGRA + dimensions + a
  bomb gate; real implementation is a thin P/Invoke wrapper over the native libwebp
  (`WebPGetInfo` for dims, `WebPDecodeBGRA` for pixels, `WebPFree` for the returned
  buffer; `WebPEncodeRGBA` is used by tests to *generate* fixtures).
- New `IconImageNormalizer` (Service, static or sealed with the decoder injected):
  - WebP magic (`RIFF` … `WEBP`) → dims via the decoder → reject max axis > 1024 or
    area > 1024×1024 → decode first frame → WPF resize to max axis ≤ 96 preserving
    aspect (skip the transform when already ≤ 96) → `PngBitmapEncoder` → PNG bytes.
  - Any other bytes pass through unchanged (WPF-native formats keep the existing
    `BoundedIconDecode` path at render time).
  - Any decoder/encode failure or gate rejection → null.
- `ServerEnrichmentService.DownloadIconAsync` normalizes after the bounded read;
  only normalized PNG bytes are cached and returned.
- Native transport: `<PackageReference Include="Imazen.WebP.NativeRuntime.win-x64"
  Version="1.6.1" />` in the launcher csproj — MIT-licensed package carrying the
  official libwebp 1.6.1 binaries (`runtimes/win-x64/native/libwebp.dll` +
  `libsharpyuv.dll`). Empirical pre-checks done: exports `WebPGetInfo`,
  `WebPDecodeBGRA`, `WebPFree`, `WebPEncodeRGBA` are present; `libsharpyuv.dll` is an
  import of `libwebp.dll` (both ship in the package). REVISED from the original
  "commit the dll" plan: the official libwebp Windows archive ships static libs only
  (no DLL) and this machine has no MSVC/CMake — the NuGet transport preserves the
  decision's substance (official libwebp binary + our own P/Invoke, ~400 KB) while
  updates ride NuGet and no binary blob enters the repo.

## Why

The new CFX endpoint serves WebP — 25/25 live samples — and WPF/WIC cannot decode it.
Normalizing at the download boundary means every downstream consumer
(`BytesToImageSourceConverter`, `BoundedIconDecode`, `ShortcutIconWriter`) stays
byte-format-agnostic and the LRU cache holds ~KB PNGs instead of multi-MB raw files.
The dims-before-decode gate continues the decompression-bomb story: the raw RGBA
buffer is allocated only after the header proves a sane canvas. First-frame-of-animated
is the deliberate degrade for animated WebP (a static thumbnail; decode failure →
null = "no icon", same slot as before).

## Scope

- `src/FiveMServerLauncher/FiveMServerLauncher.csproj`: the native transport
  `PackageReference` above (verified below for build output and single-file publish).
- New files: `Service/IWebpDecoder.cs` (+ real `LibWebpDecoder`), `Service/IconImageNormalizer.cs`.
- `ServerEnrichmentService`: normalization call in the download path.
- Tests:
  - Normalizer with a fake decoder: passthrough of non-WebP bytes; gate rejection of
    absurd canvases; decode failure → null; resize invoked for oversized canvases;
    aspect preserved (target derived from `BoundedIconDecode.MaxPixelSize`).
  - Real-fixture end-to-end: committed real CFX WebP icon
    (`tests/…/Service/Fixtures/cfx-server-icon.webp`, captured 2026-10-26 from
    `cfx-server-metadata…/7665c31f-…`, 2.8 KB, VP8X 96×96) → PNG bytes with ≤96
    axes — pins the vendored dll decoding in-process.
  - Oversize canvas fixture generated at test time via `WebPEncodeRGBA`.
  - Enrichment service: a routed WebP URL → normalized PNG comes out of
    `GetIconAsync(iconUrl)` and is what gets cached.
- Verify empirically: `dotnet build` places `libwebp.dll` beside the output; a Release
  single-file publish bundles it; note results in this file.

## Done when

- A real CFX WebP icon round-trips to PNG through the enrichment seam; garbage and
  bombs degrade to null; non-WebP bytes are untouched.
- The full suite is green and no test decodes WebP on the UI thread assumptions.

## Comments

- Verified 2026-10-26: `dotnet build` places `libwebp.dll` (+ `libsharpyuv.dll`,
  `libwebpdemux.dll`, `libwebpmux.dll`) beside the app exe in
  `bin/Debug/net10.0-windows` — the package targets copy them flat, so
  `DllImport("libwebp")` resolves for dev runs and for the test host (the
  real-fixture test decodes in-process, which is the proof). Single-file publish
  verified with the documented Release commands: `dist/CFXLauncher.exe`
  65,600,475 bytes with NO loose natives in `dist` — all four native dll names
  byte-search-positive inside the bundle (`IncludeNativeLibrariesForSelfExtract`
  extracts them at startup).
- Code review (2026-10-26) remediations folded in: the real oversize-canvas fixture
  promised here is now generated via `WebPEncodeRGBA` (2000×2000) and rejected
  through the REAL decoder; the enrichment seam gained an
  `undecodable-WebP → null and never cached` pin; the post-download continuation
  now runs on the pool via `ConfigureAwait(false)` through the icon path (the loop
  starts on the window dispatcher and browser icons load from a binding getter —
  without it the native decode + PNG re-encode landed on the UI thread).
- Animated WebP: `WebPDecodeBGRA` on animated input is expected to yield the first
  frame; the `WebPGetInfo` bomb gate reads the *canvas* dims, which is the right
  bound either way. Decode failure degrades to null = "no icon", the same slot as
  before; not further pinned.
