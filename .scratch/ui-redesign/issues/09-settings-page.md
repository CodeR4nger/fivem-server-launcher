# 09: Settings page

**What to build:** the Settings page from the mockup, scrolling, with General and Dev mode
tabs. General: Launch (Preferred client as a segmented control of installed clients — the same
state as the open-client button — and Auto-launch), Integration (web links, minimize to tray),
Display (language, Interface animations Default / Minimal), About (launcher version, Check for
updates). Each row has a title, a one-line description and a switch or control; switches are the
theme's toggle switch. Check for updates is a new command running the existing release-feed
check on demand (busy while running; result "Up to date", the title-bar pill, or "Could not
check for updates"; only when the update feature is wired). Dev mode tab: Client (FiveM /
RedM), Game build and Pure mode (existing persisted values and option engine), Launch and
Launch second client. Tab content slides in.

See `../spec.md` (Settings stories, Check for updates, Interface animations).

**Blocked by:** 04, 05.

**Status:** ready-for-agent

- [ ] Every existing setting keeps its persistence and failure-revert behaviour (existing tests pass).
- [ ] Interface animations UI drives the setting from ticket 03; Minimal turns motion off immediately.
- [ ] Check for updates tested with the release-feed fake: up to date, update found, failure, busy gate, feature unwired.
- [ ] Old settings panel and Dev Mode panel markup removed; `SettingsPanel_ShouldShowEveryCheckboxUnclipped` is deleted and replaced by a Settings page layout test (both tabs, minimum size, all 11 languages).
- [ ] New strings in all 11 languages; zero warnings.
