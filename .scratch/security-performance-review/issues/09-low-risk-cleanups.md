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

**Status:** ready-for-agent

- [ ] Registry key disposables are used at every open
- [ ] Cooldowns and per-cycle refresh honor cancellation
- [ ] Deleted/edited saved-server rows stop holding localizer event subscriptions
- [ ] Localizer state is safe for background reads by construction
- [ ] Store reads degrade on IO faults like they do on JSON faults
