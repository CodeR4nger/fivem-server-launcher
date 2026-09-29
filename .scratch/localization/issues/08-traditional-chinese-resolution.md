# 08: Decide what Traditional Chinese users get

**What to build:** `Localizer.SystemLanguageCandidates` unconditionally yields `zh-Hans` for any
`zh*` culture, so `zh-TW`, `zh-HK` and `zh-Hant-TW` all resolve to Simplified Chinese. Spec
implementation decisions say: "exact culture match, then neutral parent, else English". A Traditional
user therefore gets a script they may not read, which contradicts the stated rule.

**Why it is a ticket and not a fix:** both behaviours are defensible and the choice is a product
call, not a code smell. Option A (today): any `zh*` → Simplified, so a Traditional user reads
something rather than nothing. Option B (spec-literal): `zh-Hant-*` is not a shipped tag and not the
neutral parent, so it falls through to English — a Traditional user gets a language they definitely
read. Option C: ship a `zh-Hant` dictionary and resolve it properly.

**Blocked by:** None.

**Status:** ready-for-agent

**Severity:** low (affects a minority, and today's behaviour is not broken — only undocumented)

- [ ] Product decides between A, B and C
- [ ] `LocalizerTests` pins the chosen resolution for `zh-TW` and `zh-Hant-TW`
- [ ] The spec's resolution-order line is amended to describe the actual rule either way
