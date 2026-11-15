# Server browser sorting (review-hardening session 7, wave 3)

Source: `.scratch/review-hardening/plan.md` section 7.

## Decisions
- **Keys**: players and name, each ascending or descending — four options in ONE compact
  combo in the browser filter row (`MOST PLAYERS` / `FEWEST PLAYERS` / `NAME A–Z` /
  `NAME Z–A`), styled like the game filter combo. No clickable headers: the list has no
  header row and DOC.md's minimal UI does not want one.
- **Default**: players descending (what people browse by).
- **Persistence**: session-only (like the game/hide/search filters, which are not persisted
  either). No reason found to grow `LauncherSettings`.
- **Ties** (deterministic total order): players → name ascending → cfx id ordinal.
  name → cfx id ordinal. Tie-breakers are always ascending, whatever the main direction.
- **Name compare**: on `Name` (colour codes already stripped and trimmed), culture-aware
  (`CultureInfo.CurrentCulture` at row build time), `IgnoreCase | IgnoreSymbols`.
  Symbols, punctuation, whitespace and emoji are ignored wherever they appear, so a
  decorated `★ Alpha` sorts as `Alpha` — decoration cannot buy a top slot in NAME A–Z.
  A name with no letters/digits (blank, `???`, `~*`) sorts LAST in both name directions,
  among such names by cfx id. (Amended after the visual check: with "empty sorts first",
  hundreds of blank-looking live rows filled the top of NAME A–Z.)
- **Mechanism**: pre-sort, not `ICollectionView` sorting. Filters keep the backing order;
  `CustomSort`/`SortDescriptions` would re-sort the 33k rows on every `Refresh()` — every filter
  toggle and every (debounced) search — on the UI thread.
  - `LoadAsync` ranks names inside its existing `Task.Run` (`ServerBrowserSorting.RankNames`:
    one culture `SortKey` per row, sorted once; rows get `NameRank` (equal names share it) and
    a unique `NameOrdinal` = position in (name, cfx id) order). The keys are discarded.
  - Every sort (`ServerBrowserSorting.Sort`) packs ONE `long` per row — high 32 bits the main
    key in the chosen direction (−players / players / 0 / −NameRank), low 32 bits `NameOrdinal`
    (which IS the ascending tie-break chain) — and runs `Array.Sort(keys, rows)`: primitive
    compares, no strings, no pointer chasing, no allocation per compare.
  - The sort runs on the UI thread at the swap with the sort selected THEN, so a refresh — or a
    sort change while a load is in flight — always lands in the selected order (no race branch).
  - A sort change re-sorts the current rows and swaps the collection (same row instances: icons
    and saved markers survive; the swap re-attaches `MatchesFilters`).
- **Localization**: four new keys ×11; labels re-emit on `LanguageChanged` keeping the
  selected sort (the game-filter precedent).

## Testing
VM tests: default order, each key/direction, ties, symbol/case rules, composition with
filters, survival across a refresh (`RefreshCommand` / second `LoadAsync`), language change.
Measurement with a 33k-row synthetic catalog reported in the session notes and the report.

## Measurements (2026-11-15, live catalog ≈ 32.9k rows, Release, noisy shared machine)
- Sort change (UI thread, sort + collection swap + view rebuild): 9–20 ms warm (first-ever
  change incl. JIT ≈ 40–50 ms).
- Load/refresh with sorting on: name ranking +200–420 ms OFF the UI thread (ICU `GetSortKey`
  for every row); the keyed sort at the swap 6–20 ms on the UI thread. Total `LoadAsync` over a
  cached snapshot 300–850 ms (run-to-run noise dominates).
- Filter refresh with sorting on: 4–16 ms (unchanged: no sort runs on Refresh).
- Rejected alternatives, same data: `ListCollectionView.SortDescriptions` (players+name)
  620–2500 ms; culture string compare per comparison 210–570 ms; a typed `IComparer` over rows
  with int keys 80–120 ms (pointer chasing across 33k heap objects); a delegate-chained comparer
  150–500 ms (allocating).
