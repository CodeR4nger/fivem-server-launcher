# 04: Server icons on shortcuts

**What to build:** Shortcuts carry the server's own icon when the row has icon bytes at
creation time, falling back to the launcher icon otherwise.

**Blocked by:** 03 (modifies its creation flow).

**Status:** ready-for-agent

- [ ] Rows with icon bytes: the icon is materialized through the existing bounded decoder
     (≤96 px, any input, any aspect ratio), re-encoded as PNG, embedded in an ICO container,
     and written to the shortcut-icons folder beside the running exe (portable-data
     convention)
- [ ] Icon files are named by content hash — identical artwork dedupes to one file; orphans
     are never swept
- [ ] The shortcut references the materialized icon path
- [ ] Missing bytes → launcher icon, no fetch attempt at creation time (re-creating the
     shortcut later picks the icon up)
- [ ] A non-writable exe dir → the icon write fails → launcher-icon fallback, and the
     shortcut is still created
- [ ] The materializer is a real class with no interface (there is no surrounding logic
     class), tested with real temp-directory I/O and parse-back assertions on the ICO
     container bytes: header, entry, embedded PNG, hash dedup, and garbage/oversized input
     normalizing through the bounded decoder
