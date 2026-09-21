# 02: CitizenFX.ini config writer

**What to build:** A seam that writes `[Game]` key/value pairs into the Legacy FiveM client's `CitizenFX.ini` safely: creates the file when absent, preserves every other key and section verbatim, never touches keys outside the requested set, and only rewrites the file when a target value actually changes.

**Blocked by:** None (can start immediately).

**Status:** done

- [ ] `ICitizenFxConfigWriter` seam with a real writer that applies a key→value set to the `[Game]` section via read-modify-write.
- [ ] Creates the file (with `[Game]`) when it does not exist.
- [ ] Preserves all existing content; updates only the target keys; leaves the file untouched when every target value already matches.
- [ ] Writer behavior proven against real temp files (create / update / no-op / preserve-other-keys).
