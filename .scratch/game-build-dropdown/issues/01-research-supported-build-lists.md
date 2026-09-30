# 01: Research: authoritative supported game-build lists

**What to build:** The curated build data the whole feature transcribes into code. Consult the
CFX documentation for `sv_enforceGameBuild` (FiveM/GTA5 and RedM/RDR3) and record every build
number each platform actually supports, each with its human update name (for example
`3095 — Latest`, `2802 — Los Santos Drug Wars`), plus which entry is the current latest/default.
The answer lands in this ticket under `## Answer` and is the single transcription source for
ticket 02 — it must be traceable to the documentation page(s) it came from.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

Authoritative source: the `sv_enforceGameBuild` entry on the CFX server commands page
(https://docs.fivem.net/docs/server-manual/server-commands/#sv_enforcegamebuild-build,
fetched 2026-09-30). "Every build includes all content and changes from the builds before."
Enhanced story from the Legacy-vs-Enhanced migration page
(https://docs.fivem.net/docs/developers/legacy-vs-enhanced/#gamebuilds).

**FiveM (GTA5) builds** — verbatim from the docs table (Number / Aliases / Marketing name):

| Number | Aliases | Marketing name |
|--------|---------|----------------|
| 1 | — | Base game without any DLCs |
| 1604 | xm18, christmas2018, mpchristmas2018 | Arena War |
| 2060 | sum, mpsum | Los Santos Summer Special |
| 2189 | h4, heist4, mpheist4 | Cayo Perico Heist |
| 2372 | tuner, mptuner | Los Santos Tuners |
| 2545 | security, mpsecurity | The Contract |
| 2612 | mpg9ec | *(none published)* |
| 2699 | mpsum2 | The Criminal Enterprises |
| 2802 | mpchristmas3 | Los Santos Drug Wars |
| 2944 | mp2023_01 | San Andreas Mercenaries |
| 3095 | mp2023_02 | The Chop Shop |
| 3258 | mp2024_01 | Bottom Dollar Bounties |
| 3407 | mp2024_02 | Agents of Sabotage |
| 3570 | mp2025_01 | Money Fronts |
| 3751 | mp2025_02 | A Safehouse in the Hills |
| 3889 | mp2026_01 | The Kortz Center Heist |

**RedM (RDR3) builds** — the docs table documents exactly one entry:

| Number | Notes |
|--------|-------|
| 1491 | September 2022 update, limited content/changes. |

**Latest:** the docs table carries no explicit "latest" designation; the newest entry is 3889
(The Kortz Center Heist), and the Enhanced migration page independently calls "The Kortz Center
Heist" *the latest gamebuild* — so the curated label for the newest FiveM build is `Latest`.

**FiveM Enhanced:** confirmed — Enhanced "currently supports only the latest gamebuild (The
Kortz Center Heist)", loaded by default when no build is specified (`sv_enforceGameBuild 1`
can load the base game server-side, but the launcher never passes build flags to Enhanced and
the combo disabling per spec stands). No Enhanced build list exists to curate.

**Ambiguities and decisions for ticket 02:**

- The docs table lists *The Diamond Casino & Resort* and *Diamond Casino Heist* with no number
  and no alias (dash rows) — those updates shipped no enforceable build, so they are **excluded**
  from the curated list; there is nothing a launcher could select.
- `2612` (mpg9ec) has no published marketing name — the label should be the bare number, not
  an invented name.
- The RedM list being a single entry is the current documented reality: community wikis mention
  older RDR3 builds (1311–1487), but the CFX documentation — the authoritative source this
  feature commits to — documents only 1491. The launcher must not offer builds the platform
  does not support, so 1491-only ships.
- Aliases are recorded here for traceability only: the launcher serializes numbers
  (`-b<build>` / the connect URI), so the curated catalog carries numbers + marketing names,
  not aliases.
- Build `1` ("Base game without any DLCs") is a documented, selectable value — include it.

- [x] FiveM (GTA5) supported builds: every documented build number with its update name
- [x] RedM (RDR3) supported builds: every documented build number with its update name
- [x] The current latest/default entry is identified, as CFX labels it
- [x] Source URLs cited; any ambiguity (deprecated builds, docs disagreement, nameless builds)
      noted explicitly
- [x] A brief note confirming (or correcting) the spec assumption that FiveM Enhanced publishes
      no build list of its own — build flags simply do not apply to it

**Spec:** `.scratch/game-build-dropdown/spec.md`

## Comments

- Follow-up trace (2026-09-30, user-driven): the docs table is neither the only nor the most
  current source. Two further CFX sources were located and verified:
  - **Numbers** — `code/premake5_builds.lua` on the citizenfx/fivem `master` raw URL is the
    build-system source of truth: `five` = 1, 1604, 2060, 2189, 2372, 2545, 2612, 2699, 2802,
    2944, 3095, 3258, 3323, 3407, 3570, 3751, 3788, 3889; `rdr3` = 1311, 1355, 1436, 1491
    (a superset of the docs' single 1491); plus a GTA6-era `ny` section a parser must ignore.
    The master file is identical to commit `e34d12cd`.
  - **Names** — the "DLC:" pill (FiveM client and servers.fivem.net alike) is computed
    UI-side by `getGameBuildDLCName`, hardcoded in the CFX frontend bundle
    (`serversList-<hash>.js`, hash resolved from the page HTML): 2060→Los Santos Summer
    Special; 2189/2215/2245→Cayo Perico Heist; 2372→Los Santos Tuners; 2545/2612→The Contract;
    2699→The Criminal Enterprises; 2802→Los Santos Drug Wars; 2944→San Andreas Mercenaries;
    3095→The Chop Shop; 3258/3323→Bottom Dollar Bounties; 3407→Agents of Sabotage;
    3570→Money Fronts; 3717/3751/3788→A Safehouse in the Hills; 3889→The Kortz Center Heist;
    unknown→empty. A live `/single` probe (8e8xxv) confirmed `vars` carries only
    `sv_enforceGameBuild` — no DLC tag in the API data.
  - The client mapping resolves the docs' nameless `2612` ("The Contract") and names the
    patch builds 3323/3788. Baseline names follow the client mapping where present, the docs
    otherwise (1604→Arena War, 1→base game), bare number when no source knows (all RedM).
- User decisions recorded 2026-09-30: build numbers AND names auto-update once per launcher
  session (premake + frontend bundle) with the curated baseline as offline/outage fallback;
  no "Latest" marker on the newest build (it shows its DLC name).
