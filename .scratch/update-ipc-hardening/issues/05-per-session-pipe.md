# 05: Per-user, per-session, current-user-only connect pipe

**What to build:** Listener and forwarder derive the same default pipe name from product name + user SID + session id, and both pass `PipeOptions.CurrentUserOnly`. Spec decision 5.

**Blocked by:** none

**Status:** resolved

## Answer

New `Service/ConnectPipeName` (`For(sid, sessionId)` / `ForCurrentSession()`) is the single name source for both ends; `Local\` prefix and `DefaultPipeName` const removed; both ends pass `PipeOptions.CurrentUserOnly`. UNVERIFIED: an ACL-reading test of the server end failed (cause not investigated) and was dropped; CurrentUserOnly is therefore unpinned by tests.
