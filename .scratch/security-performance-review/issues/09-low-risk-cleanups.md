# 09: Low-risk cleanups

**What to build:** A batch of small hardening items found by the sweep:
- The registry scheme check opens a key and never disposes it (per connect).
- Cooldown delays (`Task.Delay`) are not cancellable; the enrichment loop's cancellation token does
  not reach the per-cycle work, so a closed window can still run one final refresh.
- Short-lived row VMs subscribe to the app-lifetime localizer's `LanguageChanged` and never
  unsubscribe, keeping deleted/edited rows rooted.
- The localizer's cross-thread contract (UI-thread writes, background reads) is implicit; make it
  explicit (lock or volatile) so background `Get` calls are safe by construction.
- `ReadAllText` on the two JSON stores sits outside the sanctioned catch set (an `IOException`
  from AV locks would crash startup instead of degrading to empty).

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Registry key disposables are used at every open
- [x] Cooldowns and per-cycle refresh honor cancellation
- [x] Deleted/edited saved-server rows stop holding localizer event subscriptions
- [x] Localizer state is safe for background reads by construction
- [x] Store reads degrade on IO faults like they do on JSON faults

## Landed decisions (recorded per the spec)

- Cancellation: the composition root owns one `CancellationTokenSource` cancelled on
  `window.Closed` and feeds it to both VMs (cooldown delays) and the enrichment loop. The loop's
  token reaches the per-cycle work: `RefreshServerInfoAsync(forceRefresh, cancellationToken)`
  throws at entry and per row, and the loop re-checks between the server-info and status
  refreshes — a closed window never runs one final cycle. The manual-refresh cooldowns ride the
  shared `ViewModels/Cooldown.ElapseAsync` helper; on the shutdown token the cooldown ends early
  and leaves the command disabled (harmless at shutdown). Cooldown token wiring has no dedicated
  test: the command paths are async-void `ICommand` handlers, not awaitable seams; the loop pieces
  are tested.
- Weak events: `SavedServerItem` subscribes via `WeakEventManager` (the app-lifetime localizer
  never roots a deleted/edited row). Non-rooting is pinned by the GC test; *delivery* is pinned by
  the pre-existing `StatusLabel_WhenLanguageSwitched_ShouldRefresh` test.
- Localizer: language state (`_requestedTag`/`_effectiveTag`) is only touched under a lock
  (background `Get` safe by construction); `LanguageChanged` and the injectable culture provider
  run OUTSIDE the gate so a handler/provider can never deadlock against a reader. The
  concurrency test is a well-formedness harness, not a deterministic regression guard — the
  guarantee is the lock by construction (reference reads were already atomic).
- Stores: `IOException` joins `JsonException` in the sanctioned degrade set (sharing violations
  throw `IOException`; verified by file-lock tests). `UnauthorizedAccessException` (ACL denials)
  stays out — the ticket's stated fault is the AV sharing lock; minimal set.
- Glue disposals (the registry key `using`, the `ProcessStarter` wrapper `using` — the ticket-08
  hand-off) have no dedicated tests: both are real-OS one-liners and touching the real registry
  or starting real processes in tests is forbidden; disposing the `Process.Start` wrapper never
  kills the started process.
- Waived: `refreshCts` is never disposed (no timer; process exit releases it).
