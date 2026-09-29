# 07: Make adding a language genuinely one JSON file

**What to build:** Shipping a new language today needs two code changes, not one file:
`FiveMServerLauncher.csproj` needs an `EmbeddedResource` entry for `Localization/<tag>.json`, and
`Localizer.ShippedLanguageOptions` needs a `new("<tag>", "<native name>")` row. Spec user story 11
promises "translations to be data files (no code changes), so that adding or improving a language is
one JSON file". Close the gap: wildcard-embed `Localization/*.json` in the csproj, and move the
native display name into the dictionary itself (a reserved `_displayName` key, excluded from the
key-completeness audit the same way `_note` already is).

**Why:** the display name is the one string a translator cannot ship as data today, so a community
contributor adding a language must hand-edit a C# file — exactly the barrier story 11 exists to
remove. Note this interacts with `06`: once the display name lives in the JSON,
`Localizer.ShippedLanguageOptions` stops being a hand-maintained list and can be derived from the
embedded set, which also removes the "declared but unparsable" class of bug entirely.

**Blocked by:** 06 (recommended — do the seam change first so the shipped list has one owner).

**Status:** done

**Severity:** medium (spec gap, not a defect)

- [x] csproj embeds `Localization/*.json` by wildcard; no per-language entry
- [x] Native display name read from the dictionary, not from C#
- [x] `_displayName` and `_note` excluded from the completeness audit
- [x] The audit still fails the build on a shipped-but-unparsable file (do not regress the fix in `05`)
- [x] Documented in `AGENTS.md` that adding a language is one file

## Resolution

Tags are derived from the embedded resource NAMES (so a corrupt file still contributes its tag and
stays selectable, per ticket `05`) and the label comes from that file's `_displayName`
(`Localizer.ReadLanguageOption`, falling back to the tag). Keys beginning with `_` are metadata and
are excluded from both the string dictionary and the key-completeness audit. The shipped list is
ordered deterministically (fallback language first, then by tag) because resource enumeration order
is not stable across builds.

Verified end to end by dropping a throwaway `zz-Probe.json` into `Localization/`: it appeared in the
shipped set with its own `_displayName` and no code change, and the completeness audit failed the
build naming it with every missing English key. The file was then removed.
