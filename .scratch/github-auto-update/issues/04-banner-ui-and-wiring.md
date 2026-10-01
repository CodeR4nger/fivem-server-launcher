# 04: Banner overlay UI + composition-root wiring

**What to build:** A small non-modal banner overlay in the main view, inheriting the custom
control styling: when a new version is available it shows the new version with a one-click
UPDATE button and a dismiss control; in-progress shows the update being applied; failed shows a
failed hint and stays dismissable. The composition root constructs the real release feed and
the real updater (with their real file/process seams), injects them into the view model, and
starts the check after the window is up. Every banner string is a localization key shipped in
English (source of truth) and every shipped language file — the audit test enforces
completeness.

**Blocked by:** 03 (the VM state the banner binds to).

**Status:** resolved

## Answer

Delivered as a bottom strip in `MainView.xaml`: the root grid gained `RowDefinitions`
(\* / Auto) and the banner sits in the Auto row spanning all three columns — it mounts only
while one of the three VM flags is set (`AnyTrueToVisibilityConverter`, a new view-scoped
multi-converter whose true parameterless ctor is pinned for BAML) and takes no layout space
otherwise, so nothing is occluded; the strip's own height is the only layout change. Available
shows the composed `UpdateBannerText` with the accent `MainButtonStyle` UPDATE button;
in-progress a quiet secondary line; failed a red line — the dismiss ✕ (`SecondaryButtonStyle`)
is visible on available|failed only, matching the VM's mid-flight refusal. The banner is
declared BEFORE the dialog and browser overlays (in WPF later siblings paint above — the
first draft had it last, which left it floating above the dialog scrim with clickable buttons;
caught by the spec review) and both overlays gained `Grid.RowSpan="2"`, so while either is
open the banner is fully covered and unclickable. All strings are loc keys
(`UpdateButton`/`UpdateInProgressText`/`UpdateFailedText`) shipped in all 11 languages; the ✕
is a symbol, like the existing glyph buttons. Composition root wiring: `GitHubReleaseFeed`
rides the shared 15 s client (small JSON), `UpdateApplier` gets a dedicated 10-minute
`HttpClient` (a 62 MB asset cannot fit the shared timeout), `Environment.ProcessPath` as the
exe provider (NOT `AppContext.BaseDirectory` — the single-file extraction dir), the
`SingleInstanceGuard` as the released lock and `Shutdown` as the exit action; the check starts
post-startup through the already-wired `InitializeAsync` and never blocks. Visual verification:
live E2E against the real hand-published v1.3.0 release — the user saw the banner and clicked
UPDATE (ticket 05 records the full run).

- [x] The banner renders only in available / in-progress / failed states and is hidden
      otherwise; it is never modal and never blocks the saved-servers list or the connect flow
- [x] UPDATE commands the view model's update command; dismiss commands dismiss — no business
      logic in code-behind
- [x] In-progress and failed visuals follow the VM state
- [x] Banner strings are localization keys present in `en.json` and every shipped language file;
      the localization audit is green
- [x] Any XAML-instantiated type has a true parameterless constructor (the BAML rule)
- [x] Composition-root wiring: real feed + updater injected; the check starts post-startup and
      never blocks
- [x] Suite green; visual verification by the user

**Spec:** `.scratch/github-auto-update/spec.md`
