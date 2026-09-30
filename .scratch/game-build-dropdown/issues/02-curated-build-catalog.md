# 02: Game-build option engine + curated baseline

**What to build:** The pure logic both build dropdowns read, and the data it falls back to.
An option type (`GameBuildOption(int? Build, string Label)` — the None entry is `Build = null`,
mirroring the game-client option idiom) plus an option-list engine that, given a game client,
the currently stored build, a localized None label, and a build dataset (per-game supported
numbers + a number→name map), produces the dropdown list: None first, then the supported
builds newest-first labeled `<number> — <name>` (bare number when no name is known), plus one
raw-number entry when the stored build is not in the list. FiveM Enhanced yields no options —
build flags do not apply to it. A curated baseline dataset ships in code — today's numbers
(the premake list) with names per ticket 01 (the client mapping first, the docs filling
`1604`/`1`, bare numbers for RedM) — pinned by a test asserting the baseline constants
themselves, so a bad edit fails the build loudly. The dataset is pure data, so a
runtime-fetched dataset is interchangeable with the baseline. No XAML, no fetching in this
ticket; verifiable at the domain seam.

**Blocked by:** 01.

**Status:** resolved

## Answer

Landed as three small domain types:

- `GameBuildOption(int? Build, string Label)` — the None entry is `Build = null` (mirrors the
  game-client option idiom; `SelectedValuePath` binding-ready).
- `GameBuildData(FiveM, RedM, Names)` — the pure dataset: per-game supported numbers plus a
  number→name map. Baseline and runtime-fetched data are the same shape, so they are
  interchangeable by construction (proven in tests: every behavior test builds a custom
  dataset).
- `GameBuilds` — static engine: `Baseline` (the curated constants) + `Options(game,
  storedBuild, noneLabel, data = null)` (null data falls back to the baseline — the outage
  story in one default).

Behavior decisions that emerged in the loop:

- The engine sorts numbers descending itself, so datasets are order-agnostic (a source that
  reorders its file changes nothing).
- Enhanced returns a **fully empty** list — the first GREEN attempt leaked the None entry and
  the raw stored entry for Enhanced (the combo binds an empty ItemsSource while disabled);
  the pinning test forced the early return before any list construction.
- An unlisted stored build appends exactly one entry, labeled with its **name when one is
  known** (the client mapping names dropped builds like 2215, so a stale-but-named value
  shows `2215 — Cayo Perico Heist`, not a bare number) — this was the REFACTOR step: the
  label composition is one `LabelFor` used by both the loop and the raw entry.
- Labels are `<n> — <name>` (em dash, per the spec examples) or the bare number.

Baseline contents (pinned by literals in the test): FiveM = 1, 1604, 2060, 2189, 2372, 2545,
2612, 2699, 2802, 2944, 3095, 3258, 3323, 3407, 3570, 3751, 3788, 3889 (premake five section);
RedM = 1491, 1436, 1355, 1311 (premake rdr3); names = the frontend `getGameBuildDLCName`
mapping (incl. 2612 → The Contract and the 3323/3788 patch names) with the docs filling
1604 → Arena War and 1 → Base game without any DLCs; no RedM names published.

- [x] Option-list engine: None first, newest-first supported builds, `<n> — <name>` labels,
      bare-number labels when unnamed, exactly one raw entry for unlisted stored builds,
      empty list for FiveM Enhanced
- [x] Curated baseline matches ticket 01's sources (numbers from the premake list; names from
      the client mapping with docs filling `1604`/`1`)
- [x] A pinning test asserts the curated baseline constants themselves, so a bad edit fails
      the build loudly
- [x] The dataset shape is pure data (per-game numbers + name map), interchangeable between
      baseline and runtime-fetched data
- [x] Suite green (RED → GREEN → REFACTOR) — 8 new tests, full suite 750 green

**Spec:** `.scratch/game-build-dropdown/spec.md`
