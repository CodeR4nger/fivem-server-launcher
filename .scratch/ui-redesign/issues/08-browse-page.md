# 08: Browse page

**What to build:** the server browser as a full page in the mockup's style. Header: title,
server count, search (existing debounce), refresh (existing cooldown). Filter row: game chips
(All games, FiveM, FiveM Enhanced, RedM), Hide full and Hide empty chips, and the sort control
(existing four sorts). Rows (virtualized, as today): server mark, name and address, filled game
tag, players, Connect (existing browser connect command, which now switches to the Servers page
so the status line is visible) and Save / Saved. Loading, failure and no-match states are
clear and distinct. Filters, sort, search and scroll position survive switching pages. Row
hover eases.

See `../spec.md` (Pages, Browse stories).

**Blocked by:** 04.

**Status:** done

- [x] Existing browser and sorting tests pass; tests that named the overlay are rewritten.
- [x] Connect from a row switches to the Servers page and runs the normal connect (tested at the view-model seam).
- [x] State survives page switches (tested); scroll position verified manually.
- [x] Scrolling 30k rows stays smooth (manual check on the live catalog, recorded); virtualization still on.
- [x] Layout test for the page in all 11 languages; zero warnings.

## Comments

**Delivered (2026-11-16)** on `claude/ui-08-browse-page-f11535`.

### What changed

- **View model** (`ServerBrowserViewModel`, tested):
  - `ShownCount` / `ShownCountText`: the number of rows the list shows. It is read from the
    filtered `ListCollectionView` (O(1)) after every refresh. "1 server" has its own key.
    Digits are grouped by the UI language, not the system culture: the live check found an
    English UI on a Spanish system reading "34.249 servers". `ShowsCount` hides the count
    until there are rows.
  - `IsNoMatch`: rows are loaded, but the filters and search exclude all of them.
  - `ShowsLoadFailure`: a load failed and there is nothing to show. When a reload fails over
    loaded rows, the rows now stay; before, the failure text was drawn over them.
  - `ClearFiltersCommand`: resets the game, Hide full, Hide empty and the search with one
    immediate refresh, and cancels the pending search debounce.
  - `ServerBrowserItem`:
    - `PlayersText`, formatted once on first read; it replaces the per-row `MultiBinding`;
    - `GameTagLabel`;
    - `HasGame`.
- **Page** (`Views/ServerBrowser.xaml`): page-only styles live in its own resources;
  nothing was added to `Theme.xaml`.
  - **Header:** title, shown count, search (existing 250 ms debounce) and refresh (existing
    cooldown).
  - **Filter row:**
    - the game chips are one `ListBox`, so the arrow keys move the selection;
    - a divider, then Hide full and Hide empty as `FilterChipStyle` toggles;
    - the sort dropdown, docked right.

    The chips sit in a `WrapPanel`, so a long language wraps instead of clipping. None
    wraps at 1000 px today.
  - **Rows:** raised cards with:
    - the mark (icon, or initials as a fallback);
    - the name over the address, both shortened with an ellipsis when too long;
    - the filled game tag and the player count;
    - **Connect** (the existing command, which returns to Servers) and **Save**.

    A saved server shows a quiet "Saved" that is neither clickable nor a tab stop. Save and
    Saved share one cell, so the button keeps the same width in every row and the Connect
    column never shifts.
  - **States:** exactly one shows at a time.
    - Loading: a ring that turns only while it is visible and motion is on.
    - Failure: a danger mark, a hint and **Try again**, which runs the refresh.
    - No match: a magnifier, a hint and **Clear filters**.
  - **Motion:** the row's hover layer eases in over 150 ms with motion on and snaps with
    motion off.
- **Labels:** browser labels now use the mockup's sentence case ("All games", "Most players",
  "Connect", "Saved").
  - New keys, in all 11 files (Spanish curated): `BrowserServerCount`,
    `BrowserServerCountOne`, `BrowserSaveButton`, `BrowserLoadingHint`,
    `BrowserLoadFailedTitle`, `BrowserLoadFailedHint`, `RetryButton`, `BrowserNoMatch`,
    `BrowserNoMatchHint`, `ClearFiltersButton`, `BrowserSortLabel`, `BrowserGameFilterLabel`.
  - `SaveButton` stays, for the dialog.
  - **Left for ticket 10:** `BrowseServersHeader` and `BrowserLoadFailed` are now unused.

### Performance

The list stays virtualized and recycling. A test pins this and also checks that no ancestor
sets `CanContentScroll=False`. The new row template is kept light:

- every row has a fixed height;
- the hover brush is a frozen local copy (the theme's `HoverLayerBrush` is the one theme
  brush that is not frozen);
- the mark is a local template without the theme mark's `Viewbox`;
- no row runs a heavy converter.

**How it was measured:** a throwaway probe opened the real `MainWindow` on screen against the
live streamRedir catalog (33.5k rows), in a Release build.

- **First rows:** the time from the rows being swapped into the view to the first rendered
  frame that shows them.
- **Step:** scroll 3 rows, then force layout and a render, repeated 1000 times (3,000 rows).
- **Frames:** advance 3 rows on each rendered frame, for 1000 frames.

Other sessions were building on the same machine, so the numbers are noisy. `main` and this
branch were therefore run alternately:

| | First rows after swap | Step: median / p95 / max (ms) | Frames over 33 ms (out of 1000) |
|---|---|---|---|
| `main` | 166, 143, 205 ms | 7.3–12.3 / 17–28 / 47–94 | 8, 5, 3, 4, 1 |
| this branch | 105, 170 ms | 5.5–11.0 / 10–27 / 20–69, and 163 in one noisy run | 0, 0, 4, and 16 in that noisy run |

The new rows are taller, so 6 are realized per screen instead of 10. Scrolling is at least as
smooth as on `main`. In the real app, the first rows appeared about 0.6 s after clicking
Browse, including the catalog download.

### Tests

- **New tests:**
  - 11 view-model tests;
  - 5 item test cases;
  - `MainViewModelBrowseLayoutTests`:
    - the page at 1000×640 in all 11 languages: the header, the filter row and a saved row
      with a very long name. Nothing is clipped, the name is ellipsized, and it keeps at
      least 240 px;
    - virtualization;
    - each list state showing only its own panel;
    - after a page switch, the scroll position survives, with the very same row on top;
    - Tab from a row goes to its Connect, then its Save;
    - the row hover, with motion on and off;
    - the row's accessible name.
- **Checked against mutations:** each new view test was seen failing against one:
  - buttons that are too wide;
  - `CanContentScroll` turned off;
  - a Save button that is not focusable;
  - a state bound to the wrong flag;
  - the hover trigger without its motion condition;
  - a catalog reload on every visit. The first scroll test missed this; it now also checks
    the row instance.
- **Changed helper:** `RegionProblems` (the shell layout helper) now skips the natural-width
  check for a `TextTrimming` text, which is shortened on purpose.
- **Existing tests:**
  - none named the overlay any more (ticket 04 had renamed them);
  - all existing browser and sorting tests pass unchanged.
- **Results:** the full suite passes 1461/1461 and was green on three consecutive runs.
  `dotnet build FiveMServerLauncher.slnx --no-incremental`: 0 warnings.

### Real-app check

Setup: the Debug exe, with no other launcher running. A fake `saved-servers.json` and a
settings file with `AutoLaunch: false` sat beside it and were deleted afterwards. Nothing was
connected.

- **Live catalog:** 34,249 rows loaded, with real icons (`runtime-live-catalog.png`).
- **Keyboard, through UI Automation:** Down, Down and Up move the selection between rows, and
  Tab goes to the row's Connect, then its Save.

### Screenshots (`../screenshots/08-browse-page/`)

- **Offscreen renders** of the real window with sample rows:
  - `loading`, `loaded`, `failed`, `no-match`;
  - `chip-all-games`, `chip-fivem`, `chip-fivem-enhanced`, `chip-redm`;
  - `hide-full-and-empty`;
  - `saved-row` (the saved server is row 2);
  - `maximized` (1920×1040);
  - `loaded-es`, `loaded-ru`.
- **Live capture:** `runtime-live-catalog`.

### Not verified

- How the hover ease and wheel scrolling feel to a person. Scrolling stays item-based, as on
  `main`.
- A non-100 % DPI or a second monitor.
- A Windows run with "Show animations" off. Tests cover the motion-off paths.
