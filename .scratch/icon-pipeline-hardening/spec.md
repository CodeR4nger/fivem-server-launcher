# Icon pipeline hardening

Source: project review of 2026-11-14, session 1 of `.scratch/review-hardening/plan.md`.

## Problem

Server icons are untrusted, server-published content. The pipeline delivered by
`.scratch/icon-url-webp-icons/` has five gaps:

1. **Non-image bytes crash the binding.** `IconImageNormalizer.Normalize` returns non-WebP
   bytes untouched and `BytesToImageSourceConverter` decodes without a guard: HTML bytes
   bound to `Image.Source` throw `NotSupportedException` out of the binding. The app has no
   global exception handler, so a re-bound row can crash it.
2. **Size bounds are not real.** Every request uses the default
   `HttpCompletionOption.ResponseContentRead`, so `HttpClient` buffers the whole body before
   `BoundedContent` counts a byte. `maxIconBytes` / `maxPayloadBytes` only decide whether an
   already-buffered body is kept.
3. **Responses leak.** `HttpResponseMessage` is never disposed in the catalog download, the
   icon download and `CfxService.GetServerAsync`.
4. **Failures are retried forever.** A URL that fails is re-downloaded on every per-minute
   enrichment cycle (and on every browser row realization).
5. **Any https host is fetched.** The launcher requests whatever host a server publishes.

## Host measurement (2026-11-15)

One download of the live catalog (`streamRedir`, 20,931,533 bytes), decoded frame by frame:

| | count |
|---|---|
| servers (frames, 0 corrupt) | 33,474 |
| servers publishing a non-blank `icon_url` | 25,171 |
| scheme `https` | 25,171 (100%) |
| host `cfx-server-metadata.cfx-services.net` | 25,171 (100%) |
| any other host | 0 |

Decision (per the user's rule "allowlist only if the data supports it"): **allowlist**. The
single allowed host is `cfx-server-metadata.cfx-services.net`.

## Decisions

- **Decodability is proven at download.** After normalization, non-WebP bytes are decoded
  once with the same bounded decode the converter uses (`BoundedIconDecode`, both axes
  ≤ 96 px, so validating never decodes a bomb at natural size). Bytes that decode pass
  through untouched (PNG/JPEG/GIF/BMP/ICO keep working); bytes that do not become a null
  icon. This runs where the normalizer already runs: off the UI thread.
- **The converter never throws.** Any decode failure converts to null, and the null is
  remembered in the converter's LRU so the same failing `byte[]` is not re-decoded on every
  row realization. Defence in depth: the service should never hand it such bytes.
- **One bounded download primitive.** `BoundedContent` owns "send with headers-read, bound
  the body, dispose the response". The cap aborts mid-stream; disposing the response drops
  the connection instead of draining it.
- **Overall timeout is preserved.** With headers-read `HttpClient.Timeout` stops covering
  the body, so a slow-drip body would hang the catalog single-flight forever. The primitive
  applies the client's own `Timeout` to the whole exchange (headers + body).
- **Swallow set grows by exactly one type.** A mid-body transport failure now surfaces as
  `IOException` (it used to be wrapped in `HttpRequestException` by the buffering path).
  Catalog and icon downloads treat `HttpRequestException`, `IOException` and
  `OperationCanceledException` as outages; anything else still propagates.
- **Negative cache: two classes, one bounded LRU.**
  - *Content rejections* (over the byte cap, bomb-gate rejection, undecodable) are
    deterministic for a URL, and icon URLs are self-versioned (a new icon is a new guid
    URL): remembered for the session.
  - *Transport failures* (non-success status, `HttpRequestException`, `IOException`,
    timeout) may be transient: remembered for a retry delay (default 5 minutes,
    injectable), then tried again.
  - Unexpected faults propagate and are not remembered.
  - Bounded by the existing `maxCachedIcons` (the same capacity applies to each cache;
    entries are a URL and a timestamp). An evicted failure is simply retried.
- **Host allowlist.** `IsSafeIconUrl` additionally requires the host to be the allowed one
  (case-insensitive). Redirects are still followed by `HttpClient`; they can only be issued
  by the CFX host itself and the response still passes every size/decode gate.

## Out of scope

- `ShortcutIconWriter` keeps its own copy of the bounded decode (not in this session's
  file set) — follow-up: point it at the shared `BoundedIconDecode.TryDecode`.
- `CfxService` error contract (throws on non-success) — session 5.
- Catalog User-Agent and stream decoding — session 6.
- Global exception handler — session 4.

## Testing

xUnit at the existing seams, no mocking library: `ServerEnrichmentService` and
`ServerCatalog` through `HttpClient` with fake handlers (new streaming/tracking content
fakes), `IconImageNormalizer`, `BytesToImageSourceConverter`, plus one real
`Image.Source` binding on an STA thread.
