# 04: Shell and navigation

**What to build:** the new frame around the pages. The window is resizable with a 1000×640
minimum and default, maximizable, and restores its saved size, position and maximized state
(off-screen positions fall back to centred). The custom title bar gains maximize/restore and
double-click maximize, and hosts the update notice as a pill (available / in progress / failed,
Update and dismiss, same commands and texts as the old banner). One rounded content panel
shows the current page; the bottom bar holds the page tabs (Servers, Browse, Settings) with a
sliding highlight, CFX status at the left, and a placeholder slot for the open-client button
(ticket 05). The "CFX Launcher by CodeRanger" footer is always visible, above every overlay.
The existing content is re-homed unchanged inside the pages: saved servers and the connect
field on Servers, the browser on Browse (no longer an overlay), the settings panel and Dev Mode
panel on Settings. Pages fade/slide in.

See `../spec.md` (Window, Pages, Update notice, Interface animations).

**Blocked by:** 03.

**Status:** ready-for-agent

- [ ] `CurrentPage` replaces browser-open / settings-open / dev-mode as navigation state; the browser view model survives page switches.
- [ ] Window size/position/maximized persist and restore; minimum enforced; maximize works from button and double-click; tray restore still returns to normal or maximized.
- [ ] Update pill covers all three states and dismiss rules exactly as the banner did (existing update tests still pass, rewritten only where a name became false).
- [ ] Footer stays visible above dialogs and confirm.
- [ ] Nav highlight slides; pages animate in; both off when motion is off.
- [ ] Every control reachable by Tab in a sensible order; nav tabs work from the keyboard.
- [ ] A shell layout test (minimum size, all 11 languages) passes; screenshots at minimum size and maximized recorded; zero warnings.
