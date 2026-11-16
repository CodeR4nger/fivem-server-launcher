# Changelog

## [1.4.0] (2026-11-16)

### Added
- **System tray icon** — the launcher sits in the notification area with quick-connect to your saved servers, Show and Exit. Turn on **Minimize to the system tray** in Settings to hide the window to the tray when you minimize it.
- **`cfxlauncher://` web links** — opt in under Settings and links like `cfxlauncher://connect/<address>` on websites open that server in the launcher, whether it is running or not. The launcher only ever registers or removes its own link handler.
- **Browser sorting** — sort the server browser by most or fewest players, or by name A–Z / Z–A.
- **Check for updates** — a button in Settings → About checks GitHub on demand instead of only at startup.

### Changed
- **A redesigned launcher** — the single fixed 860×520 screen with its stacked overlays is replaced by a resizable window (minimum 1000×640, maximizable) with its own title bar, one rounded content panel and a bottom bar with three pages: **Servers**, **Browse** and **Settings**. The launcher remembers its size, position and maximized state, and comes back on screen if the monitor it was on is gone.
- **Servers page** — your saved servers on the left with search and All / Online / Offline filter chips showing live counts; the selected server's details on the right (players, address, game build with its DLC name, pure mode, Steam/Discord requirements) with one amber **Connect** beside the address. Every server action — edit, desktop shortcut, copy address, move up/down, delete — lives in one ⋮ menu; dragging a row still reorders the list.
- **Browse page** — the server browser is now a full page instead of a sliding overlay, with game chips, Hide full / Hide empty, sort, a live "N servers" count, and clear empty, loading and failed states (with Try again). Filters, search, sort and scroll position survive switching pages.
- **Settings page** — grouped rows with switches in two tabs: **General** (preferred client, auto-launch, web links, minimize to tray, language, interface animations, version and **Check for updates**) and **Dev mode** (client, game build, pure mode, launch / launch second client).
- **Open client button** — the bottom bar's split button opens your preferred client, and its ▾ menu (opening upward) changes which one; it reads "No client installed" instead of an empty box when nothing is detected.
- **Update notice in the title bar** — a small pill replaces the bottom banner.
- **Dialogs** — add/edit server and confirmations are centred modals over a dimmed window, with keyboard support (Esc cancels, Enter saves, focus returns where it was); delete confirms in red.
- **Motion and look** — page fades, a sliding tab highlight, modal and menu pops, gliding switches and hover feedback, all short and eased. A new **Interface animations: Minimal** setting turns them off, and they also follow Windows' "Show animations" setting. The UI now uses the bundled Inter font with higher-contrast text, and offline servers show a grey dot (red stays for RedM and errors).
- **Keyboard and screen readers** — every page, the tabs (arrow keys), menus and dialogs can be used without a mouse, Tab moves through each row in reading order, and every control and option is announced by name.
- A web link, desktop shortcut or tray connect now brings up the Servers page, so you see how the connect is going.

### Fixed
- **Saved servers and settings can no longer be wiped** — if a file is locked (for example by an antivirus) or unreadable, the launcher refuses to overwrite it instead of saving an empty list; a corrupt file is backed up as `.bak` first, and entries it cannot read are kept rather than dropped. A failed save shows a message instead of crashing.
- **Connecting during a CFX outage** — a `cfx.re/join/` address now still connects through FiveM when CFX's server lookup is down, instead of showing "Invalid address".
- **Same settings whichever address you use** — joining by IP:port or domain applies the same game build and pool settings as joining by cfx id.
- **Accented install paths stay intact** — the launcher no longer corrupts non-English characters (like `José`) in FiveM's `CitizenFX.ini`.
- **Pasted addresses with spaces or line breaks** now connect instead of failing as invalid.
- **No more stuck "Resolving…"** — an unexpected error during connect, open or dev launch ends with an error message.
- **A server whose icon is not a real image** can no longer crash the launcher.

### Security
- **Verified updates** — an update is only installed when its download matches the SHA-256 digest GitHub publishes, and only from github.com.
- **Shortcut and link forwarding is private to your Windows user and session**, and a client that never sends anything can no longer block it.
- **Server icons are only fetched from CFX's own icon host**, and every download is size-limited while it streams instead of after.
- **Join links are validated strictly**, so text placed before `cfx.re/join/` can no longer slip into a desktop shortcut.

### Performance
- **No background downloads while you play** — the server list (about 20 MB) is no longer re-downloaded every few minutes while the launcher is minimized or in the background, about 250 MB an hour less.
- **Lower memory use** — the server list is decoded as it downloads, about 70 MB less per refresh.

## [1.3.1] (2026-10-26)

### Fixed
- **Server icons load again** — CFX moved server icons off the icon-version field the launcher was reading onto a new per-server icon URL served as WebP, a format the launcher could neither see nor decode. The launcher now reads the new field, downloads the image directly and converts it natively, so icons are back in the saved list, the server browser and desktop shortcuts — including oversized images that the old download limit silently dropped.

## [1.3.0] (2026-10-03)

### Added
- **Quick-connect desktop shortcuts** — right-click any saved server and choose "Create desktop shortcut": double-clicking the new shortcut on your desktop connects straight into that server, with no launcher window and no button press. The shortcut runs the same validated pipeline as a manual connect — server data, Steam/Discord started and waited until fully ready, launch — so a shortcut connect is never a second-class connect. The shortcut carries the server's own icon on the desktop (the launcher icon when the server has none), and if the launcher is already running, the shortcut brings it forward and connects through it. Note: firing a second shortcut while the first game is still loading shows a FiveM client error (its own single-instance handoff racing its startup) — once the game has loaded, shortcuts switch servers normally.
- **Row action menu** — right-click anywhere on a saved server row, or the ⋮ button on the selected row, opens one dark context menu with every row action: Edit server, Create desktop shortcut, Delete server. Right-clicking also selects the row, so the menu always acts on the server you clicked.
- **Delete confirmation** — deleting a saved server now asks first ("Delete server "X"?"), so one misclick in a menu can never remove it.

### Changed
- The footer's big DELETE button is gone — deleting lives in the row's action menu now, and BROWSE fills the freed space next to + NEW SERVER.
- The edit pencil became a ⋮ kebab — same selection-only visibility, same one-click idiom as the other glyph buttons.
- Re-creating a shortcut after editing a server updates it in place, with a confirmation before overwriting an existing shortcut of the same name.

## [1.2.0] (2026-09-30)

### Added
- **Launcher self-update** — on startup the launcher checks GitHub for a newer release and shows a small dismissible banner at the bottom of the window. One click downloads the new version and restarts the launcher into it — fully portable, no elevation, no manual file juggling; the replaced exe is kept as a backup and cleaned up by the next update. Updates are never forced and never downgrade: only properly tagged published releases apply, and a failed or interrupted update leaves the running launcher exactly as it was.
- **Game builds by name** — the Dev Mode and local-dev game-build boxes are now dropdowns listing the builds FiveM and RedM actually support, labeled with their DLC names (for example `[3095] The Chop Shop`; RedM builds show their number). The list refreshes itself from CFX's own sources on every launch — a new GTA DLC appears without waiting for a launcher release — and falls back to the built-in list when offline. Previously saved build numbers, even retired ones, keep loading and launching unchanged.
- **Reorder saved servers** — hover or select a row to reveal up/down arrows in the margin next to the list; or drag a row straight onto a new slot, guided by an accent drop line. The order persists across restarts and never applies while the search filter is active.
- **UI localization** — 11 languages, defaults to the system language, live switching from Settings, English fallback guaranteed. Adding a language is a single JSON file: its native name lives in the file rather than in code, so a community translation needs no source change.
- **Localhost support** — `localhost`/`127.0.0.1` work as direct connect addresses; saved loopback servers can carry manual parameters (CFX ID, game build, pure mode, game) in a dedicated local-dev panel, with a resolvable CFX ID winning over manual values.
- **Resilient id connects** — if a cfx id can't be resolved (server delisted, CFX outage), the launcher falls back to the saved direct address instead of failing.
- **Dark controls everywhere** — every dropdown and checkbox now renders in the app's dark theme instead of the white Windows default: dark popups with the thin scrollbar, amber hover and a subtle selected state; dark boxes with an amber check; matching keyboard-focus and disabled states.
- **Motion across the app** — dropdowns expand out of their origin and collapse away again; moving a saved server (arrows or drag) slides the rows into their new slots instead of jump-cutting; settings and Dev Mode crossfade in and out (the status area and split button fade inversely, nothing shifts position mid-fade); the add/edit server dialog fades in and out over the window; the loopback local-dev panel slides in from the right and back out; the server browser slides across the window on open and close. Every motion is short and eased, and a rapid reopen reverses smoothly instead of snapping.
- **Browser loading feedback** — opening the server browser while the catalog is still downloading shows a localized "Loading servers..." line instead of a silent empty list; it never flashes over existing rows.

### Changed
- **One glyph-button idiom** — the two refresh buttons, the edit pencil and the move arrows share a single style: identical hover feedback and one normalized disabled state, so a cooling-down refresh stays visible-but-dimmed instead of nearly invisible.
- **Hidden servers are never matched by address** — typing a local address connects to your server, not a stranger's hidden one.
- **Bounded network waits** — 15 s HTTP, 5 s DNS, capped download sizes; the UI can no longer freeze for minutes.
- **Atomic writes** — saved servers, settings and `CitizenFX.ini` can no longer be corrupted (or wiped) by a crash mid-write.
- **Enrichment request hygiene** — the server catalog is downloaded once and shared by every surface (connect, saved list, browser) instead of per-caller; the per-minute saved-list refresh no longer probes CFX per row (the catalog snapshot is the single source of icon versions), and icon downloads are shared, concurrency-bounded and LRU-cached — the launcher is a far better CFX API citizen.
- **Smooth server browser** — search filtering debounces while typing instead of re-filtering the 30k-row list per keystroke, saved-row markers use keyed lookups, and icons decode at a bounded pixel size with decoded-image reuse, so crafted server icons can no longer blow up memory.
- **Traditional Chinese systems read Simplified Chinese** — `zh-Hans` is the only Chinese dictionary we ship, so a `zh-TW`/`zh-HK` locale no longer falls through to English; shipping a Traditional dictionary later is a data-only addition.

### Fixed
- Editing a saved server's address kept it in place on screen but scrambled its position after a restart.
- Saving a server from the browser kept its raw connect endpoint — it now stores the server's cfx id as the connect address, so the entry keeps working when the server moves host, and it can no longer create a duplicate of a server that is already saved by its cfx id.
- The language picker showed its names in the wrong language at startup when a non-English language was already saved.
- A saved browser game filter the launcher no longer offers could crash the browser at startup.
- A language file that fails to parse no longer silently vanishes from the picker — it stays selectable and renders through the English fallback.
- A malformed server response could crash the launcher.
- A disk fault during the background id capture could fail a server save.
- Startup survives installation/IO faults with a status message instead of crashing.
- Server icons load again in the browser and the saved list — the catalog publishes the icon version as a typed field (any non-zero value, including negative, is valid), not a server variable.
- Closing the window no longer runs one final refresh cycle; every cooldown and per-row enrichment step honors shutdown.
- Native handle leaks closed — process readiness probes (per poll), the URI scheme registry check (per connect) and shell-started processes no longer leak handles.
- Deleted/edited saved rows no longer stay in memory through the localizer's event; language switching is now safe for background reads by construction.
- An antivirus lock on `saved-servers.json` or `launcher-settings.json` degrades to empty/defaults instead of crashing startup.

### Security
- Command-injection hardening: strict cfx-id charset everywhere, manual ids validated on save, escaped executable paths — crafted `saved-servers.json` entries can no longer smuggle shell arguments.
- `CitizenFX.ini` injection guard — a malicious server can no longer smuggle keys or sections into the game config through a crafted pool-sizes value; poisoned entries are skipped instead of written.
