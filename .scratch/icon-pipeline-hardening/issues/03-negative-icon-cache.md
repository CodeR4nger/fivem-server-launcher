# 03 — Bounded negative icon cache

Status: open

## What

`ServerEnrichmentService` remembers failed icon URLs in a bounded LRU (capacity
`maxCachedIcons`): content rejections for the session, transport failures for
`iconRetryDelay` (default 5 minutes, injectable; time via the injected `TimeProvider`).
A remembered failure answers null without a request. Unexpected faults are not remembered.

## Tests

- Undecodable / over-cap URL requested twice → one request (replaces the old
  "ShouldReturnNullWithoutCaching" claim test, deleted and rewritten).
- 404 and thrown `HttpRequestException`: second request inside the delay → no new request;
  after the delay → requested again, and a now-healthy icon is served.
- Content rejection is not retried after the delay.
- Capacity: with `maxCachedIcons: 1`, a second failing URL evicts the first, which is
  requested again.
