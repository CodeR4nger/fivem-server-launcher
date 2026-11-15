# 04: Bound the release-feed body

**What to build:** The feed reads at most 1 MB of response body (headers-first); a larger body yields no update. Spec decision 4.

**Blocked by:** none

**Status:** resolved

## Answer

The feed requests headers-first and reads the body through the shared `BoundedContent.ReadAsByteArrayAsync` with a 1 MB cap (`MaxBodyBytes`; real body ≈ 5 KB); over the cap → null. Headers-first moves body-stream faults out of `HttpRequestException`, so `IOException` (`HttpIOException`) joined the sanctioned swallow set — pinned by a mid-body failure test. Note: `BoundedContent` is shared with the icon session's files; this ticket only calls it.
