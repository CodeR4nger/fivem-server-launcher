# 05: Per-user, per-session, current-user-only connect pipe

**What to build:** Listener and forwarder derive the same default pipe name from product name + user SID + session id, and both pass `PipeOptions.CurrentUserOnly`. Spec decision 5.

**Blocked by:** none

**Status:** open
