# 03: Sweep stale staged downloads

**What to build:** The applier's best-effort housekeeping also removes `<exe>.new-*` files left by a crashed update; a locked one never blocks. Spec decision 3.

**Blocked by:** none

**Status:** resolved

## Answer

The sweep moved into `UpdateApplier.SweepStaleFiles` and covers `<exe>.new-*` next to `<exe>.old-*`, still before this update stages its own file and still best-effort (a locked stale staged file never blocks — pinned by a test).
