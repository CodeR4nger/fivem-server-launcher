# 06: INI value injection guard

**What to build:** The server-published `sv_poolSizesIncrease` string is written verbatim into
`CitizenFX.ini`; embedded newlines can inject arbitrary keys/sections into FiveM's config. The
config writer rejects line breaks in keys and values (degrading the write instead of corrupting
the file).

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Values containing line breaks (or keys) are rejected/skipped by the writer
- [x] Legitimate multi-line-free values write exactly as before
- [x] The existing preserve-everything-else behavior is unchanged

## Landed decisions (recorded per the spec)

- The guard lives in the writer — the single choke point where untrusted data meets the file
  (the preparer passes `sv_poolSizesIncrease` verbatim; any other future caller is covered too).
  Filtering runs before any I/O; all-unsafe input degrades to "don't touch/create the file".
- Skip-vs-reset trade-off: a poisoned `sv_poolSizesIncrease` is skipped entirely, so a stale
  value from a previously connected server persists until the next clean connect. Substituting
  an empty value was rejected: "what to write" policy belongs to `CitizenFxPreparer` and the
  generic writer cannot know per-key defaults; the stale value is a benign pool-size bump with no
  security or crash impact. If the reset ever matters, the right fix is a preparer-level sanitize.
- Only CR/LF are treated as line breaks — U+2028/U+2029/NEL pass the guard (no mainstream Windows
  INI reader treats them as line terminators); waived as out of scope.
