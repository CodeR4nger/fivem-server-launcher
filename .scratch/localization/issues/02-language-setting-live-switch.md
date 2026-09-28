# 02: Language setting with live switching

**What to build:** A language picker in the Settings panel: "System default" plus every shipped
language with its native name. The choice persists in the global portable settings (null means
follow system) and applies immediately — the whole UI re-renders without restart. Choosing
"System default" restores follow-system behavior.

**Blocked by:** 01 (localizer core with English dictionary).

**Status:** ready-for-agent

- [ ] Settings panel exposes the language picker (System default + shipped languages)
- [ ] Picking a language activates it immediately (live, no restart)
- [ ] The choice persists across restarts through the settings repository (immediate-save
      pattern like the other preferences)
- [ ] "System default" stores the follow-system sentinel and resolves via the culture provider
