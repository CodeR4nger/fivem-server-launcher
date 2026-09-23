# v1.1 Phase 11 — Saved-servers search bar

Status: ready-for-agent

## Problem Statement

Once the saved-servers list grows, finding one server means scrolling and eyeballing rows.
There is no way to filter the list.

## Solution

A search box above the saved-servers list that filters the rows live as you type, matching
against server name and address (case-insensitive substring). Empty query shows everything.

## User Stories

1. As a user, I want to type part of a server's name and see only matching servers, so that I
   can find one quickly.
2. As a user, I want the search to also match the address, so that pasting a fragment of an
   IP/domain finds the server.
3. As a user, I want filtering to happen as I type, with no extra key or button.
4. As a user, I want search to be case-insensitive, so that I don't have to remember casing.
5. As a user, I want clearing the box to bring back the full list immediately.
6. As a user, I want selection/edit/delete to keep working on filtered rows exactly as
   before, so that nothing about managing servers changes.
7. As a user, I want enrichment (players/status/icons) to keep updating while a filter is
   active, so that visible rows never go stale.

## Implementation Decisions

- One always-visible TextBox placed directly above the ListBox in the saved-servers panel;
  no toggle. A subtle placeholder/hint ("Search servers") consistent with existing styling.
- Filtering is view-layer over the existing `SavedServers` collection: a
  `CollectionViewSource`/`ICollectionView` with a `Filter` predicate owned by `MainViewModel`
  (property `ServerSearchText`; refresh the view on change). Rows remain `SavedServerItem`;
  no new item type.
- Predicate: case-insensitive substring on `Name` OR `Address`. No fuzzy matching, no cfx id
  (internal key), no game tag (that's the browser view's job, phase 12).
- Selection semantics: filtering does not mutate the repository or row state; if the selected
  row is filtered out, `SelectedServer` becomes null (WPF's natural behavior) and the
  address box keeps its current text.
- Enrichment loop and manual refresh keep applying to all rows regardless of the filter;
  the view just hides non-matching ones.
- Business logic in the ViewModel; XAML only binds the TextBox and the items source.
- The search text is session-only, never persisted.

## Testing Decisions

- ViewModel-level tests: setting `ServerSearchText` filters the projected view (name match,
  address match, case-insensitivity, non-match excluded, empty text restores all).
- Filtering does not reorder or drop the underlying collection; enrichment application still
  finds rows by identity while filtered (prior art: `MainViewModelTests` presence-application
  cases).
- Prior art: none for ICollectionView in this repo — keep the surface minimal and test the
  predicate + view refresh, not WPF internals.
- TDD per repo standard: RED → minimal GREEN → mandatory REFACTOR.

## Out of Scope

- Searching the global server catalog (that's phase 12).
- Search history, highlighting of matched text, regex/advanced syntax.
- Persisting the last search.

## Further Notes

- Independent of phases 9-10; can be implemented in any order after them.
