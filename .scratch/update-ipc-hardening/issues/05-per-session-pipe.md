# 05: Per-user, per-session, current-user-only connect pipe

**What to build:** Listener and forwarder derive the same default pipe name from product name + user SID + session id, and both pass `PipeOptions.CurrentUserOnly`. Spec decision 5.

**Blocked by:** none

**Status:** resolved

## Answer

New `Service/ConnectPipeName` (`For(sid, sessionId)` / `ForCurrentSession()`) is the single name source for both ends; `Local\` prefix and `DefaultPipeName` const removed; both ends pass `PipeOptions.CurrentUserOnly`. Server-side \`CurrentUserOnly\` is pinned by \`ConnectRequestListenerTests.Start_ShouldGrantThePipeToTheCurrentUserOnly\`: a client opens the inbound pipe with \`WriteData | ReadPermissions\` (an \`InOut\` client is refused on an inbound pipe, which is why the first attempt failed) and reads the DACL. Verified to fail without the flag, where the default DACL also grants Everyone \`S-1-1-0\` and Anonymous \`S-1-5-7\`. The forwarder-side flag (the client checks the server's owner) can't be observed without a pipe owned by another user, so it stays untested glue.
