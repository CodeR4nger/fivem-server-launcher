# 02 — Real size bounds, disposed responses

Status: done

## What

- `BoundedContent.GetBytesAsync(HttpClient, HttpRequestMessage, maxBytes)`: sends with
  `HttpCompletionOption.ResponseHeadersRead`, returns null on a non-success status or a body
  over the cap (abandoned mid-stream), disposes the response, and applies the client's
  `Timeout` to headers + body.
- `ServerCatalog.DownloadSnapshotAsync` and `ServerEnrichmentService.DownloadIconAsync` use
  it; both add `IOException` to their outage swallow set (and widen
  `TaskCanceledException` to `OperationCanceledException`).
- `CfxService.GetServerAsync` disposes its response.

## Tests

- Icon and catalog: a body ten times the cap served from a counting stream is abandoned
  near the cap (bytes pulled ≤ cap + one chunk), result null.
- Icon and catalog: a stream that throws `IOException` mid-body → null, no throw.
- Icon and catalog: a body that never completes → null once the client timeout elapses.
- Icon, catalog, `CfxService`: the response content is disposed after the call (success
  and failure paths).
