# 05: Narrow ServerCatalog's catch to network failures

**What to build:** `ServerCatalog.GetServersAsync` catches a full `catch (Exception)`: correct for "outage → no match" (phase requirement), but it also masks programming errors (handler bugs, corrupt JSON from another origin, etc.). Narrow it to the expected network exceptions: `HttpRequestException` and `TaskCanceledException`. Any other exception propagates to the caller (degradation does NOT apply: it's a bug, it must be visible).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `GetServersAsync` catches only `HttpRequestException` and `TaskCanceledException` → returns no match (outage behavior intact).
- [x] An unexpected exception (e.g. `InvalidOperationException` in the handler) propagates, not swallowed.
- [x] New test in `ServerCatalogTests`: handler that throws a non-network exception → `LookupByEndPointAsync`/`LookupByIpPortAsync` throws (does not return null).
- [x] Existing outage test (`throwOnSend: true` → HttpRequestException) stays green.
- [x] Full suite green.

## Comments
