# 04: Connect flow applies requirements + readiness

**What to build:** connecting carries the requirement story end to end. The connect flow takes the target (typed address or a selected saved server), applies the saved server's manual Steam/Discord flags into resolution (merged with published `sv_enforceSteamAuth`), checks readiness of the required apps, and surfaces the result in the status: required + running → "Steam ✓ / Discord ✓"; required + missing → "Requires Steam (not running)". Launching behaves exactly as today once the status is ready.

**Blocked by:** 02 (Effective Steam/Discord requirements), 03 (App requirement readiness detection)

**Status:** ready-for-agent

- [ ] Connect uses the selected saved server's address and manual flags when present, typed address otherwise.
- [ ] Status text reflects each required app's readiness (ready ✓ / not running) for servers that require it.
- [ ] "Don't bother if everything is ready": nothing extra is shown when no requirements apply or all are running.
- [ ] `MainViewModel` connect-flow tests with fake resolver, fake readiness, fake launcher.
- [ ] Suite green.