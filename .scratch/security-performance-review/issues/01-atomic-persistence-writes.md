# 01: Atomic persistence writes

**What to build:** Saved servers, settings, and CitizenFX.ini are rewritten with plain
`WriteAllText`-style writes today; a crash or power loss mid-write truncates the file, and for
the JSON stores the next load treats the corruption as empty and silently wipes all data on the
next save. All three stores write atomically (temp file + replace), with no leftover temp files.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Saved-servers, settings, and CitizenFX.ini writes survive interruption (temp + replace)
- [x] A save leaves exactly the target file (no temp residue)
- [x] Existing corrupt-file degradation behavior stays intact (load empty, never crash)
