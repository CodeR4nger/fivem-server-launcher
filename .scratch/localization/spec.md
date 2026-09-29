# Feature: UI text localization (configurable, defaults to system language)

Status: ready-for-agent

## Problem Statement

Every visible string in the launcher is hardcoded English (XAML labels, view-model status
messages, tags, tooltips). Non-English-speaking users — a large share of the FiveM community —
get an English-only product, and there is no mechanism to change that.

## Solution

A localization system with one embedded string dictionary per language, a language picker in the
Settings panel that defaults to the Windows system language, and live switching: the UI re-renders
immediately when the user picks a language. Eleven languages ship; English is the guaranteed
fallback for any missing key or file.

## User Stories

1. As a Spanish-Windows user, on first run I want the UI in Spanish, so that I can use the
   launcher without knowing English.
2. As a user whose system language has no shipped dictionary, I want English, so that the UI is
   always complete.
3. As a user, I want a language picker in Settings offering "System default" plus every shipped
   language with its native name, so that I can choose confidently.
4. As a user, when I pick a language I want the whole UI to switch immediately, so that I see the
   effect without restarting.
5. As a user, I want my choice persisted, so that it survives restarts.
6. As a user, choosing "System default" again I want follow-system behavior restored, so that the
   launcher tracks my Windows language.
7. As a user, I want every visible string localized: main buttons (settings, dev mode, open,
   enter server), section headers (saved servers, connect address, CFX status, dev controls),
   dialog labels/titles/buttons (new/edit server, name, address, requires Steam/Discord, save,
   cancel), dialog errors (invalid name or address, server already saved), status messages
   (resolving, launching, opening, not installed, launch failed, invalid address, server saved,
   starting/failed app preparation), row tags (offline, unresolved, players/max label parts, game
   tags), CFX status labels, browser chrome (back, browse servers, filters, hide full/empty,
   search placeholders, refresh tooltips), and settings panel copy.
8. As a user, if a language is missing a key, I want the English string shown, so that I never
   see a blank. (A key missing from *English itself* is a developer error, not a translation gap;
   the raw key is shown deliberately so the offender is identifiable in the UI. See
   "Missing key" under Implementation Decisions.)
9. As a user, if a language file is corrupt or missing entirely, I want the app to run in
   English, so that a bad translation never breaks the launcher.
10. As a user, I want interpolated statuses ("Starting Steam...", "FiveM is not installed",
    "Opening FiveM...") localized with their parameters, so that dynamic messages are correct
    too.
11. As a translator/community member, I want translations to be data files (no code changes),
    so that adding or improving a language is one JSON file.
12. As a maintainer, I want English to be the source of truth for the key set, so that a test can
    prove every shipped language covers every English key.
13. As a user of Dev Mode, I want its labels localized too, so that the whole surface is
    consistent.
14. As a user, I want the language setting stored in the global portable settings file, so that
    it behaves like every other launcher preference.

## Implementation Decisions

- One new seam: a localizer service resolving key -> string for the current language, exposing
  the current language, the available language list, and a language-changed event. It is owned
  and wired by the composition root and injected into the view models.
- Storage: one embedded JSON resource per language file per language. English defines the key
  set (source of truth); shipped languages: English, Spanish, French, German, Italian, Japanese,
  Korean, Polish, Portuguese, Russian, Simplified Chinese. Keys are stable identifiers, not
  English text.
- English + Spanish are curated; the other nine ship as best-effort community-improvable drafts
  (file headers say so; the UI shows them normally).
- Language resolution order: explicit setting wins; when null ("System default"), an injectable
  system-culture provider decides (same seam pattern as the registry/path seams); exact culture
  match, then neutral parent, else English.
- Global settings gain a nullable language field, persisted through the existing settings
  repository (immediate-save setter pattern like the other toggles). No per-server data.
- Live switching: view models expose localized strings as properties and re-emit them on the
  language-changed event; XAML static labels use a thin localization markup extension that
  subscribes to the same event and refreshes. Status messages resolve at emission time.
- Missing key -> English lookup; corrupt/missing language file -> English dictionary for that
  language; none of it throws to the caller.
- "Missing key" is scoped to the *selected* language. When English itself lacks the key, `Get`
  returns the raw key. That is deliberate: English is the key-set source of truth, so a key absent
  from `en.json` is a defect a maintainer must see and fix, and the raw key names it in the UI
  instead of rendering blank. The completeness audit test is the build-time guard for that case.
- A shipped language stays *selectable* even when its file failed to parse: it renders through the
  English fallback rather than disappearing from the picker, because silently dropping the user's
  explicit choice and reverting them to their system language is worse than an English UI. The
  declared shipped set (`Localizer.ShippedLanguageOptions`) is therefore the single source of truth
  for what is offered and what a requested tag may resolve to — not the set of dictionaries that
  happened to parse.
- No RTL/flow-direction support and no locale-aware date/number formatting (the UI has none
  worth formatting).

## Testing Decisions

- Good tests assert external behavior: resolved strings per language, fallback behavior, language
  enumeration and display names, system-culture mapping, persistence round-trip, and view-model
  string emission/refresh.
- Modules under test: the localizer (pure dictionary logic: key resolution, per-language
  selection, English fallback on missing key, corrupt-file degradation, completeness audit of
  every English key in every shipped file, change event), settings persistence (existing
  in-memory storage), and view models (dialog labels/status strings emitted in the selected
  language; properties refresh after a language switch).
- The system-culture provider is injected in tests (no real OS culture dependency).
- The XAML markup extension is thin glue over the tested localizer (same stance as existing
  converters), with one exception: its indexer binding is unit-tested, because binding a plain
  property path instead of the indexer renders every label empty and nothing else in the suite
  would catch it. That test is the regression pin for the bug that shipped in the first cut.
- Prior art: in-memory settings storage, time-provider seam, existing view-model suites.

## Out of Scope

- RTL languages, locale-aware formatting, and mirroring.
- Translating server-provided content (project names, hostnames from CFX).
- Per-server or per-profile language overrides.
- Downloadable/loose-file language packs (embedded only; can be revisited later).
- Translation tooling/queues; drafts are improved by PRs.

## Further Notes

- Order: this is the second v1.2 feature, after the localhost direct-connect work; the strings
  it introduces will be born localized (keys defined once here).
- The current literal strings double as the initial English dictionary contents; the migration is
  mechanical and the English UI must be byte-identical in meaning after it.
