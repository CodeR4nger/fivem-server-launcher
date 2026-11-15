# UI redesign

Status: ready-for-agent

Rebuild the launcher's interface around a page-based layout modelled on the patterns of the
in-game GTA V Social Club overlay (rounded content panel, bottom pill navigation, list + detail,
grouped settings rows with switches, modal dialogs), keeping CFX Launcher's own identity.

Approved clickable mockup (private to the owner):
https://claude.ai/artifact/8NoRpHhYcr67Jf44mkDUTA — the reference for layout, spacing, colors,
tags and motion. Where this spec and the mockup disagree, this spec wins (decisions taken after
the mockup are recorded under Implementation Decisions).

## Problem Statement

The launcher has outgrown its single fixed 860×520 screen. Every feature added since v1.1 —
the server browser, Dev Mode, web links, the tray setting, the update banner, the local-dev
side panel, the confirm dialog — was stacked onto that one screen as another overlay or another
row in an already full column. The visible consequences:

- Settings no longer fit: the web-links and tray checkboxes were clipped until a stop-gap fix
  let the settings panel cover the CFX status block while open.
- Two buttons compete to be the main action (the big OPEN split button and ENTER SERVER), and
  the OPEN button renders as an empty box when no client is detected.
- The left column is mostly empty space when settings are closed, while the saved-server list
  on the right is cramped.
- Much of the text is small, low-contrast grey on near-black.
- The window cannot be resized, so nothing can grow.
- The view is one ~2,400-line XAML file with ~750 lines of code-behind, which makes every
  visual change risky.

## Solution

A resizable window (minimum 1000×640) with a custom title bar, one rounded content panel and a
bottom navigation bar with three pages — **Servers**, **Browse** and **Settings** — plus an
always-visible footer.

- **Servers** is a list + detail page: saved servers on the left (search, All/Online/Offline
  filter chips with counts, rows with icon, name, filled game tag and status), the selected
  server's detail on the right (players, address, published game build, pure mode,
  requirements) with a ⋮ actions menu, and a single primary amber **Connect** action beside the
  address field.
- **Browse** is the server browser as a full page instead of an overlay: game chips, Hide
  full / Hide empty, sort, search, and rows with Connect and Save.
- **Settings** is a scrolling page of grouped rows with switches, in two tabs: **General**
  (launch, integration, display, about) and **Dev mode** (the former Dev Mode panel).
- The bottom bar holds the **Open client** split button (open the preferred client, ▾ to change
  which one), the three page tabs with a sliding highlight, and CFX status on the left.
- The update notice moves to a pill in the title bar.
- Motion throughout (page fades, sliding nav highlight, modal pop, menu drop, switch glide,
  hover and press feedback), with an **Interface animations: Default / Minimal** setting.
- Inter is bundled as the UI font.

## User Stories

### Window and shell

1. As a player, I want the launcher window to open at a comfortable size (1000×640), so that nothing is clipped or cramped.
2. As a player, I want to resize and maximize the window, so that I can see more servers on a large monitor.
3. As a player, I want the launcher to remember its size, position and maximized state, so that it opens the way I left it.
4. As a player, I want the window never to shrink below 1000×640, so that no page can be cut off.
5. As a player, I want minimize, maximize/restore and close buttons in the custom title bar, so that the window behaves like any other Windows app.
6. As a player, I want to double-click the title bar to maximize or restore, so that the window follows Windows conventions.
7. As a player, I want a bottom navigation bar with Servers, Browse and Settings, so that each area has the whole panel instead of fighting for space.
8. As a player, I want the active page highlighted in the bar, so that I always know where I am.
9. As a keyboard user, I want to reach the navigation and every control with Tab and activate them with Enter/Space, so that I can use the launcher without a mouse.
10. As a player, I want "CFX Launcher by CodeRanger" always visible at the bottom, including while a dialog is open, so that the author credit is never hidden.
11. As a player, I want CFX status for FiveM, FiveM Enhanced and RedM always visible at the bottom left, so that I can tell whether a problem is on my side or CFX's.

### Open client

12. As a player, I want a clearly labelled "Open FiveM" button (named after my preferred client), so that it is obvious it starts the game client.
13. As a player, I want a ▾ next to it that lists my installed clients, so that I can switch which client it opens.
14. As a player, I want picking a client from that list to change my Preferred client setting, so that the button and Settings never disagree.
15. As a player, I want changing Preferred client in Settings to update the button immediately, so that both places always show the same client.
16. As a player, I want only installed clients offered, so that I can never pick one that cannot open.
17. As a player with no client installed, I want the button to say so and be disabled instead of rendering empty, so that I understand why nothing opens.

### Servers page

18. As a player, I want my saved servers listed with their icon, name, game tag and status, so that I can recognise them at a glance.
19. As a player, I want servers without an icon to show coloured initials, so that every row still has a recognisable mark.
20. As a player, I want the game tag to keep the filled, coloured style (FiveM amber, FiveM Enhanced blue, RedM red), so that the game is as easy to spot as before.
21. As a player, I want All / Online / Offline chips with live counts, so that I can see what is playable right now.
22. As a player, I want to search saved servers by name or address, so that I can find one in a long list.
23. As a player, I want to select a server and see its detail on the right, so that I can check it before connecting.
24. As a player, I want the detail to show current players, address, the game build the server enforces (with its DLC name), its pure mode and what it requires (Steam, Discord), so that I know what to expect before joining.
25. As a player, I want offline and unlisted servers to say so plainly in the detail, so that I am not confused by missing numbers.
26. As a player, I want one prominent amber Connect button beside the address field, so that the main action is unmistakable.
27. As a player, I want to type or paste any address into that field and connect, so that I can join servers I have not saved.
28. As a player, I want the status line under Connect to show progress (resolving, starting Steam, launching) and errors, so that I know what the launcher is doing.
29. As a player, I want a ⋮ menu on the selected server with Edit, Create desktop shortcut, Copy address, Move up, Move down and Delete, so that every action lives in one place.
30. As a player, I want to drag servers to reorder them, with a visible drop line, so that I can keep my favourites on top.
31. As a player, I want reordering disabled while a search or filter chip is active, so that I never reorder a partial list by accident.
32. As a player, I want a "+" button to add a server and a refresh button with the existing cooldown, so that those actions stay one click away.
33. As a player with no saved servers, I want an empty state that points me to "+" and Browse, so that I know how to start.

### Dialogs

34. As a player, I want the add/edit server dialog as a centred modal with a close ✕, Name and Address fields and Requires Steam / Requires Discord switches, so that it matches the rest of the app.
35. As a player adding a localhost server, I want the local connection parameters (cfx id, game, build, pure mode) to appear inside the same dialog, so that I do not lose the local-dev feature.
36. As a player, I want validation errors shown inline in the dialog, so that I can fix them without losing what I typed.
37. As a player, I want delete and shortcut-overwrite confirmations in the same modal style, so that dangerous actions still ask first.
38. As a keyboard user, I want Esc to close dialogs and menus and focus to return where it was, so that dialogs are not traps.

### Browse page

39. As a player, I want the server browser as a full page, so that it has room and I can switch back and forth without losing my place.
40. As a player, I want game chips, Hide full, Hide empty, sort and search together at the top, so that I can narrow 30,000+ servers quickly.
41. As a player, I want each row to show icon, name, address, game tag and players, with Connect and Save buttons, so that I can act directly from the list.
42. As a player, I want already-saved servers marked as Saved, so that I do not add duplicates.
43. As a player, I want a clear loading state and a clear failure state, so that an empty list never looks like "no servers".
44. As a player, I want the browse page to keep its filters, sort, search and scroll position when I switch pages, so that I can compare with my saved list.

### Settings page

45. As a player, I want settings in grouped sections with a title and a one-line description per setting, so that I understand each one.
46. As a player, I want switches instead of checkboxes, so that on/off reads at a glance.
47. As a player, I want Preferred client as a segmented control of my installed clients, so that I can change it in one click.
48. As a player, I want Auto-launch, Open cfxlauncher:// web links and Minimize to tray under the new sections, so that existing settings keep working.
49. As a player, I want the language picker under Display, so that I can change the UI language.
50. As a player, I want an Interface animations setting (Default / Minimal), so that I can turn motion off.
51. As a player, I want Default to follow Windows' own "show animations" setting, so that the launcher respects my system preference.
52. As a player, I want an About section with the launcher version and a Check for updates button, so that I do not have to restart to look for an update.
53. As a developer, I want Dev mode as a Settings tab with Client, Game build, Pure mode and Launch / Launch second client, so that it keeps working without cluttering the main screen.

### Updates

54. As a player, I want an available update shown as a pill in the title bar with an Update button and a dismiss, so that it is visible without taking space from the page.
55. As a player, I want the pill to show progress and failure states, so that I know what happened after clicking Update.

### Motion and polish

56. As a player, I want pages, the server detail and settings tabs to fade and slide in, so that changes feel smooth rather than abrupt.
57. As a player, I want the navigation highlight to slide between tabs, so that it is clear where I moved.
58. As a player, I want dialogs to pop in and menus to drop in, so that I can see where they came from.
59. As a player, I want switches to glide and buttons to give hover and press feedback, so that the UI feels responsive.
60. As a player, I want motion short (≈150–350 ms) and never blocking input, so that animations never slow me down.
61. As a player who reads other languages, I want every new label in all 11 shipped languages and every page laid out without clipping in each of them, so that the redesign works for everyone.

## Implementation Decisions

### Decisions taken after the mockup
- **Window**: resizable, minimum and default size 1000×640, maximizable. Size, position and
  maximized state persist in `LauncherSettings` (new fields) and are restored on start;
  a stored position that is off every current monitor falls back to centred. The custom
  `WindowChrome` title bar stays and gains a maximize/restore button and double-click
  maximize. The tray's restore-to-previous-state behaviour keeps working.
- **Font**: Inter is bundled as an embedded WPF resource (static weights 400/500/600/700/800 or
  the variable font, whichever renders correctly in WPF) and set once at the window root. Its
  SIL Open Font License text ships in the repo beside the font files and is mentioned in the
  README. Fallback is Segoe UI.
- **Server detail data**: `ServerPresence` gains the published game build
  (`sv_enforceGameBuild`), pure mode (`sv_pureLevel`) and Steam enforcement
  (`sv_enforceSteamAuth`), read from the same catalog snapshot through the shared `CfxVars`
  mapping (one mapping, no new requests). The detail's build label reuses the `GameBuilds`
  name data ("3258 · Bottom Dollar Bounties"; bare number when unnamed). Not published →
  "Server default" / "Off". A loopback server shows its manual values. Requirements shown =
  the saved server's manual flags merged with the published Steam flag, by the same rule
  `ServerRequirements.ForConnection` uses (manual Steam is additive).
- **Open client button**: one Preferred client setting, two editors. `SelectedOpenClient` and
  `PreferredClientOption` merge into one VM state: the bottom bar's split button (main part:
  "Open <client>"; ▾: installed clients, picking one saves it as preferred and launches
  nothing) and the Settings segmented control both read and write it. Not installed → not
  offered (a preferred client that is no longer installed falls back to the first installed
  one, as `InitializeAsync` does today). No client installed → the button reads "No client
  installed" and is disabled. The CFX status dot is not on this button; CFX status lives only
  at the bottom left. No right-click behaviour. The button is a secondary style so Connect
  stays the only amber primary action on the Servers page.
- **No hint text**: the mockup's "Opens FiveM" label at the bottom right is dropped; the
  button's own label says what it does.

### Structure
- **Prefactor first** (no visual change): split the single view into one control per area —
  shell (title bar, nav, footer, status), servers page, browse page, settings page, server
  dialog, confirm dialog — and move shared brushes, styles and templates into a theme resource
  dictionary loaded once. Code-behind moves with the control it serves. The existing layout
  test must stay green throughout.
- **Theme**: one set of tokens (ground, panel, surface, raised surface, borders, text
  primary/secondary/tertiary, accent amber, game tag colours, status colours, radii, spacing,
  type ramp) used by every control. Text contrast is at least 4.5:1 on its background,
  including small grey labels. Game tag colours stay exactly as today (`#F5A623`,
  `#4DA3FF`, `#E05252`) with near-black bold text, filled, small corner radius.
- **Reusable controls/styles**: primary and secondary pill buttons, icon button, filter chip
  (with optional dot and count), segmented control, toggle switch (a styled `ToggleButton`,
  so it stays keyboard- and screen-reader-accessible), game tag, server mark (icon with
  initials fallback, deterministic colour from the name), card, text input, dropdown,
  section header, settings row (title, description, control), modal, popup menu, empty state.
- **Pages**: a single `CurrentPage` enum on `MainViewModel` (Servers, Browse, Settings)
  replaces `IsServerBrowserOpen`, `IsSettingsOpen` and `IsDevMode` as navigation state;
  Settings has its own `SettingsTab` (General, DevMode). Browse keeps its
  `ServerBrowserViewModel` alive across page switches (filters, sort, search and scroll
  survive). The browser's catalog loads on first visit, as it does on first open today.
- **Saved-server filter chips**: `SavedServerFilter` (All, Online, Offline) on the VM with live
  counts; Offline = every row that is not online (offline and not listed). The filter composes
  with the search text in the existing collection view. `IsReorderAvailable` additionally
  requires the filter to be All.
- **Selection**: the Servers page always has a selected server when the list is not empty
  (first row on start, the neighbour after a delete). Selecting fills the Connect field, as
  today.
- **Actions menu**: the ⋮ menu replaces the row kebab and the row right-click menu (their
  fragile popup bindings and close-guard code-behind go away). Its commands are the existing
  row-scoped ones plus two new: Copy address (clipboard through a small seam so it is
  testable) and Move up/down (the existing move commands). Drag-to-reorder with the drop line
  stays; the gutter arrows go.
- **Dialogs**: the add/edit dialog and the confirm overlay keep their VM contracts
  (`IsServerDialogOpen`, `RequestConfirmation`, …) and are restyled as modals; the loopback
  local-connection parameters become a section inside the dialog (shown only for loopback
  addresses) instead of a side panel. Esc cancels; focus is trapped inside while open and
  restored on close.
- **Update notice**: the bottom banner becomes a title-bar pill bound to the same three
  mutually exclusive states and commands; the banner's text keys are reused.
- **Check for updates**: a new `CheckForUpdatesCommand` runs the existing release-feed check
  on demand (busy while running, then "Up to date" / the update pill / "Could not check for
  updates"). It is gated like the startup check (only when the update feature is wired).
- **Interface animations**: `LauncherSettings.InterfaceAnimations` (`Default` | `Minimal`,
  default `Default`). Effective motion = Default AND Windows' client-area animation setting
  on. Motion is implemented as shared storyboards/transitions in the theme; one switch
  (an inheritable flag on the window root) disables all of it. Durations ≤ 350 ms, standard
  ease-out; nothing waits for an animation to finish before accepting input.
- **Localization**: every new string is a key in all 11 language files (the audit test
  enforces it); keys orphaned by the redesign (old banner/OPEN/dev-panel/checkbox labels that
  no longer render) are removed in the cleanup ticket. VM-built labels re-emit on language
  change, as today.
- **What does not change**: connect, open, resolve, enrichment, update, tray, web links,
  shortcuts, persistence and IPC behaviour; the composition root wiring apart from new
  settings; `RelayCommand`/`AsyncRelayCommand` semantics; the hard design constraints in
  `AGENTS.md`.

## Testing Decisions

- Test external behaviour at the existing seams: `MainViewModel` (and
  `ServerBrowserViewModel`) for every state, command and setting; the enrichment service and
  `CfxVars` for the new presence fields; `FileSettingsStorage` round-trip for the new
  settings. XAML stays thin glue, as `AGENTS.md` requires.
- **Layout tests**: each page (and the dialog) gets a test that opens the real window at the
  minimum size in every shipped language, shows the page, and asserts nothing in it is
  clipped (no control past its container's bounds, no label wider than its box). Prior art:
  `SettingsPanel_ShouldShowEveryCheckboxUnclipped` (renders `MainWindow` with a real VM on an
  STA thread). That test is replaced by the Settings page's version when the old panel is
  removed (delete-and-rewrite under a new name, per the repo rule).
- **Converter/style tests** where logic exists (server mark initials and colour, game tag
  brush), following the existing converter tests, including the parameterless-ctor
  reflection test for every XAML-instantiated type.
- **No pixel or screenshot assertions.** Visual checks are made by rendering screenshots
  during development and by the manual check at the end of each page ticket, recorded in the
  ticket.
- Prior art for VM tests: `MainViewModelTests` partial files (`…PersistenceTests`,
  `…WebLinksTests`, `…IdleTests`), `ServerBrowserSortingTests`; for presence:
  `ServerEnrichmentServiceTests` with `RoutedHttpMessageHandler` and `TestProtobufFrames`.
- The suite stays green and warning-free after every ticket; CI runs on each pushed branch.

## Out of Scope

- Rockstar or Social Club branding, artwork, logos or proprietary fonts — patterns only.
- A light theme, per-user accent colours or custom themes.
- Server banners or background artwork behind the panel (a blurred backdrop is expensive in
  WPF; the panel sits on a flat ground).
- New launcher behaviour beyond what the pages need (favourites, history, player lists,
  server pinging). Those are separate features.
- Changing the tray menu's look (it stays the native Windows menu).
- Browser cards/grid view: the 30k-row list stays a virtualized list.

## Further Notes

- The mockup's sample server names, counts and builds are illustrative only.
- The redesign changes how the app looks in screenshots; the README's screenshots, if any,
  are refreshed in the cleanup ticket.
- Execution: one session per ticket in its own worktree, following
  `.scratch/review-hardening/plan.md`'s wave model; tickets whose files overlap run
  sequentially. The ticket files record the order.
