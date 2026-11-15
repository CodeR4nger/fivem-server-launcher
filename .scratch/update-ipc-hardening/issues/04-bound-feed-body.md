# 04: Bound the release-feed body

**What to build:** The feed reads at most 1 MB of response body (headers-first); a larger body yields no update. Spec decision 4.

**Blocked by:** none

**Status:** open
