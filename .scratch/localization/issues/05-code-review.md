# 05: Code review of the localization feature

**What to build:** Run the two-axis code review (Standards + Spec) over the localization feature
diff — fixed point `898508f` (the commit before `feat(localization): localizer seam...`) through
`0926e8f` (the label-binding + settings-panel fixes), spec and tickets in
`.scratch/localization/`. Report findings, fix the agreed ones. Context: the feature shipped
without the review pass and one real bug escaped (`LocExtension` bound a property path instead of
the indexer — every label rendered empty), which is exactly what this pass should catch the
remnants of.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Two-axis review (repo standards + localization spec) over `git diff 898508f...0926e8f`
      run per the code-review skill, both sub-agents reporting separately
- [x] Findings triaged: fixed in this session or ticketed with severity for a later session
- [x] Suite green after any fixes; findings recorded in this file under `## Comments`

## Comments

### Standards axis — hard violations

**S1 — `MainViewModel` switched language before subscribing (FIXED).** `MainViewModel.cs:71-75` built
`LanguageOptions` under the system language, then called `SetLanguage` (which raises
`LanguageChanged`), then subscribed. The `LanguageOptions` labels are localized strings, so a saved
`es` on an English system left the "System default" row in English, next to the Spanish language
names. Now subscribed before `SetLanguage`. Regression pin:
`Constructor_WithPersistedLanguage_ShouldLabelThePickerInThatLanguage`.

**S2 — `ServerBrowserViewModel` re-found its filter with `Single` (FIXED).**
`ServerBrowserViewModel.cs:57` would throw `InvalidOperationException` out of the event handler on an
unmatched game, and used a different shape than `MainViewModel`'s re-selection. Now
`FirstOrDefault(...) ?? GameFilterOptions[0]`. Not reachable with today's fixed option set — this is
hardening plus DRY, not a live bug.

**S3 — a corrupt language file removed the language from the picker (FIXED).** `Languages` filtered on
the *parsed* dictionaries, so an unparsable `es.json` made "Español" disappear entirely and a user who
had explicitly chosen it silently reverted to their system language. Spec: "corrupt/missing language
file -> English dictionary for that language". `Localizer.ShippedLanguageOptions` is now the declared
set that `Languages`, `SetLanguage` and system matching all key off; the English fallback renders the
language. See also P1.

**S4 — `AGENTS.md` claimed `LocExtension` is not unit-tested (FIXED).** The diff added
`Views/LocExtensionTests` in the same breath. The doc was wrong, not the test — the test is the
regression pin for the empty-labels bug that motivated this ticket.

**S5 — `DefaultLocalizer` service locator (TICKETED → 06, medium).** Static `Lazy<ILocalizer>` pinned
to a hardcoded `"en-US"`, used as the default of an optional parameter on `CfxStatusItem`,
`SavedServerItem` and `ServerBrowserViewModel`. Only tests reach it, but a `DefaultLocalizer` instance
is a *second* localizer that never gets `SetLanguage` calls, so a view model built without one would
lock to English and never re-emit on a live switch.

**Clean:** no hardcoded user-visible English left in XAML or the view models (only the brand names
FiveM / FiveM Enhanced / RedM, correctly untranslated); no business logic in the XAML glue — both
`LocExtension` and `LocalizationSource` are pure binding infrastructure.

### Standards axis — judgement calls (no action)

- **Event leak:** `SavedServerItem` strong-subscribed to the app-lifetime localizer and rooted every
  deleted/edited row — already fixed at HEAD by `WeakEventManager`. `CfxStatusItem` still
  strong-subscribes, but there are only three app-lifetime instances; benign.
- **`_requestedTag` (FIXED, found during REFACTOR, not by either sub-agent).** `Localizer` wrote the
  requested tag inside the state lock and never read it. Dead state on the hot path; removed.
- **Three near-identical option records** with inconsistent field order, **bare `string?` language
  tags**, and **`DialogGameClientOptions` re-spelling the brand names** that
  `InstalledClientOption.DisplayNameOf` owns → ticketed together in 09 (low).
- **Non-localizable parameter:** `_localizer.Format("StatusOpeningClient", ...DisplayNameOf(client))`
  injects an English brand into a localized template. Correct as-is; the brand must not be translated.

### Spec axis

**P1 — corrupt file removed the language (FIXED, same change as S3).**

**P2 — the completeness audit could pass vacuously (FIXED).** The audit iterated
`dictionaries.Keys` — exactly the set that shrinks when a file fails to parse or embed — so a dropped
`fr.json` passed green while the spec's promise ("a test can prove every shipped language covers
every English key") silently did nothing. It now iterates the declared shipped list, fails on a
shipped-but-unparsable file, and reports every missing key per language in one message.

**P3 — "adding a language is one JSON file" is false (TICKETED → 07, medium).** Needs both a csproj
`EmbeddedResource` entry and a `ShippedLanguageOptions` row holding the native name; the display name
is the one string a translator cannot ship as data.

**P4 — "System default" label built before the persisted language applied (FIXED, same change as S1).**

**P5 — Traditional Chinese gets Simplified (TICKETED → 08, low, product call).**
`SystemLanguageCandidates` maps any `zh*` culture to `zh-Hans`, so `zh-Hant-TW` renders Simplified
against the spec's "exact match, then neutral parent, else English".

**P6 — settings-panel layout tweaks in `0926e8f` (`VerticalAlignment`, ComboBox `Height`/`Margin`)
are in no ticket (NOTED, no action).** Incidental `content-sized settings panel` cleanup; the commit
message covers it.

**P7 — `Get` returns the raw key when English lacks it (ACCEPTED, no action).** Contradicts "never see a
blank or a raw key", but it is deliberate and test-pinned
(`Get_WhenKeyMissingEverywhere_ShouldReturnKey`); a key missing from `en.json` is a developer error the
raw name surfaces.

**Clean and verified:** missing key → English; corrupt/empty JSON skipped without throwing;
`es-AR → es` and `zh-CN → zh-Hans` resolution; nullable `Language` persisted through the settings
repository with an immediate-save setter; no leftover hardcoded strings; eleven dictionaries with
`_note`/`es` curated; the XAML indexer path plus the `Item[]` notification fix; the view models'
re-emit lists are complete for their localized properties.

### Triage summary

| # | Finding | Axis | Severity | Outcome |
|---|---------|------|----------|---------|
| S1/P4 | Subscribe after `SetLanguage` — picker labels in the old language at startup | both | high | fixed + pinned |
| S3/P1 | Corrupt dictionary silently drops the language from the picker | both | high | fixed + pinned |
| P2 | Completeness audit could pass vacuously | spec | high | fixed + pinned |
| S4 | `AGENTS.md` claimed `LocExtension` untested | standards | low | fixed (doc) |
| S2 | `Single` in the language-changed handler | standards | low | fixed (hardening) |
| — | `_requestedTag` dead state (REFACTOR finding) | — | low | fixed |
| S5 | `DefaultLocalizer` service locator | both | medium | → 06 |
| P3 | Adding a language is not one file | spec | medium | → 07 |
| P5 | `zh-Hant` resolves to Simplified | spec | low | → 08 (product call) |
| — | Option-record/brand-name/tag duplication | standards | low | → 09 |
| P6 | Unticketed layout tweaks in `0926e8f` | spec | none | noted, no action |
| P7 | `Get` returns the raw key as a last resort | spec | none | accepted |

Suite: 698 tests green (was 694; +4 new regression pins).
