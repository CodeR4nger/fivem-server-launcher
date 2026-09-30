# Feature: Browser loading feedback

Status: done (single-ticket feature; suite 742 green; visually verified by the user)

## Problem Statement

Opening the server browser can take a few seconds when the streamRedir catalog cache is cold
or expired: the browser slides in over an empty list and gives no feedback at all until the
33k-server snapshot finishes downloading. Only a *failure* state exists (`LoadFailed`); the
in-flight state is invisible, so the wait reads as slowness or breakage.

## Solution

An honest loading state, exactly the case DOC.md's UX philosophy sanctions ("show an
explanatory progress state when something needs waiting"): a centered, localized
"Loading servers..." line while the initial fetch is in flight, shown only while the list is
empty (a re-fetch over existing rows never flashes it) and never alongside the failure text.

## User Stories

1. As a user, when I open the browser and the list is still downloading I want to see that
   it is loading, so the wait reads as work in progress rather than an empty result.

## Implementation Decisions

- The browser view model owns the state: `LoadAsync` raises `IsRefreshing` around the fetch
  (owning it only when not already refreshing, so the manual-refresh cooldown window — which
  keeps the flag up — is untouched), and exposes a computed `IsLoadingOverlay`
  (`IsRefreshing && no rows && !LoadFailed`) with change notifications from the
  refreshing/servers/load-failed setters.
- The view centers the localized line over the list area, mirroring the existing
  `BrowserLoadFailed` placement; it can never co-show with the failure text or stale rows.
- One new localization key (`BrowserLoading`) added to every shipped language file — the
  completeness audit enforces it across all eleven.

## Testing Decisions

- VM-level tests with a gated fake HTTP handler (the fetch blocks on a
  `TaskCompletionSource`): the overlay shows while the first fetch is in flight and clears
  when it lands; a re-fetch over existing rows never shows it; an outage clears the flag and
  leaves only the failure state. The XAML text placement is manual-verification glue.

## Out of Scope

- Persisting the catalog across runs, startup prefetching, or any change to the
  download/cache pipeline — the wait itself is unchanged, only its visibility.

## Further Notes

- Surfaced during the ui-custom-controls visual passes: the new browser slide spotlights the
  previously silent empty state.
