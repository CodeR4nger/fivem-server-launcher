# 06: INI value injection guard

**What to build:** The server-published `sv_poolSizesIncrease` string is written verbatim into
`CitizenFX.ini`; embedded newlines can inject arbitrary keys/sections into FiveM's config. The
config writer rejects line breaks in keys and values (degrading the write instead of corrupting
the file).

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Values containing line breaks (or keys) are rejected/skipped by the writer
- [ ] Legitimate multi-line-free values write exactly as before
- [ ] The existing preserve-everything-else behavior is unchanged
