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

**Status:** ready-for-agent

- [ ] Existing browser and sorting tests pass; tests that named the overlay are rewritten.
- [ ] Connect from a row switches to the Servers page and runs the normal connect (tested at the view-model seam).
- [ ] State survives page switches (tested); scroll position verified manually.
- [ ] Scrolling 30k rows stays smooth (manual check on the live catalog, recorded); virtualization still on.
- [ ] Layout test for the page in all 11 languages; zero warnings.
