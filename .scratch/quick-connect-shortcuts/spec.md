# Feature: Quick-connect desktop shortcuts (--connect entry + row action menu)

Status: ready-for-agent

## Problem Statement

Players connect to the same one or two servers every single session. Today that means: open
the launcher, find or type the server, press ENTER SERVER — the same three-step ritual every
time, for a launcher whose entire job is "get me into my server with zero friction". There is
also no way to reach the game except *through* the launcher window, and per-row actions are
scattered (a selection-only pencil, a footer DELETE button acting on the selection).

## Solution

Two halves delivered together:

1. **Desktop quick-connect shortcuts.** Any saved server can be pinned to the desktop as a
   one-double-click shortcut. Double-clicking it starts the launcher with an explicit connect
   address; the launcher runs the *full validated connect pipeline* — resolve, saved-server
   requirement merge, Steam/Discord prep (started and waited until fully ready), launch —
   without anyone pressing ENTER SERVER. If the launcher is already running, the shortcut
   brings its window forward and forwards the connect to it.
2. **Row action menu.** The per-row affordances consolidate into one context menu — Edit
   server / Create desktop shortcut / Delete server — reachable by right-clicking anywhere on
   the row or via a visible kebab button on the selected row. Delete gains a confirmation,
   and the footer DELETE button goes away.

## User Stories

1. As a player, I want to double-click a desktop shortcut for my server, so that I connect
   without opening the launcher and pressing ENTER SERVER.
2. As a player, I want the shortcut to run the exact same validated connect pipeline as a
   manual connect (resolve, saved-server requirements, launch), so a shortcut connect is
   never a second-class connect.
3. As a player whose server requires Steam, I want Steam started and waited-until-logged-in
   before the game launches, so the connect does not fail on an unprepared client — the
   differentiator vs a raw `fivem://` shortcut.
4. As a player, I want the launcher window to appear with live status during a shortcut
   connect, so I always see what is happening.
5. As a player, I want the launcher to stay open after a shortcut connect (success or
   failure), so its behavior matches every manual connect.
6. As a player, when a required app cannot be prepared, I want the existing "Could not start
   {app}" status instead of a launch, so I know why nothing happened.
7. As a player, when the address embedded in my shortcut is invalid (server changed,
   shortcut edited), I want the existing "Invalid address" status, so nothing new breaks.
8. As a user, I want the shortcut to carry my server's own icon on the desktop, so I can
   find it visually.
9. As a user, when the server has no icon available at creation time, I want the shortcut to
   fall back to the launcher icon, so it is never blank or broken.
10. As a user, I want to re-create a shortcut after editing a server (name or address
    changed), so the desktop stays in sync — overwriting is the update mechanism.
11. As a user, I want a confirmation before overwriting an existing same-named shortcut, so
    a collision never silently stomps anything.
12. As a user, I want to delete a saved server from the row's action menu, so the footer
    under the list stays minimal.
13. As a user, I want a confirmation before deleting a saved server, so one misclick in a
    menu can never destroy it (menu items sit next to each other; the old dedicated button
    was one deliberate click, a menu is prone to mis-aiming).
14. As a user, I want right-click anywhere on a row to open the action menu, so I do not
    have to hit a small icon target.
15. As a user, I want the same menu reachable from a visible ⋮ kebab button on the selected
    row, so the actions are discoverable without knowing about right-click.
16. As a user, when the launcher is already running and I double-click a shortcut, I want
    the running window brought forward and the connect performed by it, so the shortcut
    always works.
17. As a user, when the launcher is already connecting and a forwarded connect arrives, I
    want it ignored, so I never get a confusing double launch or surprise delayed action.
18. As a user, I want shortcut creation to never crash the launcher (unwritable desktop, icon
    write failure, shortcut system error), so creating a shortcut is always safe.
19. As a user, I want shortcut creation to work even while the launcher is busy connecting,
    so I do not have to wait.
20. As a user with auto-launch enabled, I want an explicit shortcut connect to take
    precedence over the remembered last-server auto-launch, so the shortcut does exactly
    what it says.
21. As a user, I want a successful shortcut connect to still update the "open automatically
    next time" memory, so auto-launch stays coherent.
22. As a non-English user, I want the menu items, statuses and confirmations in my language,
    so the new surface is consistent with the rest of the UI.

## Implementation Decisions

- **CLI entry**: `--connect <address>`, parsed in the composition-root startup from the
  startup args (currently ignored). Last flag wins, unknown args ignored, zero parse-time
  validation — the connect pipeline owns validation, so a stale/edited shortcut degrades to
  the existing "Invalid address" status. The address is forwarded into the view model's
  initialize method as an optional parameter; the in-line connect reuses the auto-launch
  precedent (fill address, await the shared connect method). Explicit arg skips the
  auto-launch block; on success the last-server memory is still persisted.
- **Session choreography**: identical to a manual connect — window shows, connect runs
  in-line with live status, window stays open on success and failure. No exit choreography.
  The startup update check still runs afterwards (harmless coexistence).
- **Forwarding to a running instance (IPC)**: a named pipe under the `Local\` namespace
  (same-user session, matching the single-instance mutex naming), one UTF-8 address line
  per connection.
  - First instance: an accept loop owned by the composition root, started after
    single-instance guard success and cancelled on exit (the mutex-disposal lifecycle).
    An incoming address is marshaled to the UI dispatcher and handed to a public view-model
    method following the browser-connect precedent: silently ignored when busy (no queueing),
    otherwise fill address + connect; transient UI states (dialog open, dev mode, browser
    overlay) are not micro-managed.
  - Duplicate: after the guard fails and the existing window is restored/foregrounded by the
    existing activator, a pipe client with a ~2 s timeout writes the address; any failure
    exits silently exactly as today. The guard runs before everything, so the duplicate can
    never reach the view model directly — the pipe is the only channel.
- **Row interaction**: the edit pencil is replaced by a `⋮` kebab (selection-only visibility,
  icon-button style). Left-click on the kebab opens the row's context menu programmatically;
  right-click anywhere on the row selects the row and then opens the same menu — existing
  selection-based commands keep working unchanged. Menu: **Edit server / Create desktop
  shortcut / Delete server**, in that order, uniform dark styling via a new dark context-menu
  style (red stays reserved for failure status). The footer DELETE button is removed; BROWSE
  and NEW SERVER remain (the panel self-heals).
- **Confirmations**: one confirm-overlay mechanism (a minimal sibling of the edit-dialog
  overlay, same DataTrigger pattern; view-model state: open flag, text, confirm command
  executing a captured action). Two uses: shortcut overwrite ("A shortcut named X already
  exists. Overwrite?") and delete ("Delete server X?"). CANCEL reuses the existing dialog
  key. Delete always confirms (no smart-skip reading foreign shortcuts).
- **Shortcut payload**: the raw saved address embedded as `--connect "<address>"`; target is
  the running exe path (never the single-file extraction dir). Name is the server name
  sanitized for filename validity; same-name collision triggers the confirm overlay, then
  overwrite. Creation is not gated by the busy flag and never crashes: success and failure
  are status-line feedback ("Shortcut created" / "Could not create shortcut").
- **Shortcut icon**: the server's icon when the row has bytes — decode via the existing
  bounded icon decoder (≤96 px, any input, any aspect ratio), re-encode as PNG, embed in an
  ICO container (PNG-in-ICO), written to a `shortcut-icons` folder beside the running exe
  (portable-data convention), file named by content hash (identical artwork dedupes; orphans
  are never swept). Missing bytes → launcher icon (the shortcut simply uses the target exe's
  embedded icon); no fetch attempt at creation time — re-create later to pick the icon up.
  Non-writable exe dir → icon write fails → launcher-icon fallback, shortcut still created.
- **New seam (the only one)**: a shortcut-creator service — create shortcut at a path with
  target, arguments and optional icon path. Real implementation via WScript.Shell COM
  (dynamic dispatch — the app's first COM interop; trimming is off so the self-contained
  single-file build is safe); an IShellLink P/Invoke implementation is plan B if COM
  misbehaves. Desktop path and icon folder arrive through injectable function providers
  (the established portable-directory pattern). The icon materializer is a real class (no
  interface — there is no surrounding logic class); IPC listener and forwarder are real
  classes with no interfaces, tested directly against a real in-process pipe pair.
- **Localization**: ≈8 new keys × 11 languages (3 menu items, 2 statuses, 2 confirm texts,
  CONFIRM), `en.json` as source of truth, the completeness audit test enforces them.

## Testing Decisions

- Good tests assert external behavior only: statuses emitted, repository state, creator/args
  recorded, files on disk, pipe roundtrips — never call order for its own sake.
- **View-model tests** (existing seam, existing fakes — repository, enrichment, resolver
  chain, localizer, settings storage): startup connect with/without the arg; auto-launch
  precedence; forwarded connect (busy → ignored, idle → connect); shortcut creation happy
  path and every fallback (fake creator records target/args/icon path); overwrite confirm
  flow; delete confirm flow; status emissions; last-server persistence on success.
- **Shortcut creator**: fake in view-model tests; the real COM implementation is thin glue,
  untested per the process-starter convention.
- **Icon materializer**: real temp-directory I/O tests (config-writer precedent) with
  parse-back assertions on the ICO container bytes (header, entry, embedded PNG), hash
  naming dedup, and garbage/oversized input normalizing through the bounded decoder.
- **IPC listener + forwarder**: direct tests against a real in-process named pipe pair —
  roundtrip, timeout, malformed line, cancellation. No interfaces; no network beyond the
  loopback pipe.
- **Confirm overlay**: view-model state transitions tested like the edit-dialog pattern.
- **XAML glue** (menu open, right-click select, kebab positioning): untested per house
  convention; all commands live in the view model. The localization audit picks up the new
  keys automatically.

## Out of Scope

- Copy-join-link row action (considered, not requested).
- Tray icon, watching while the launcher is closed, slot-free notifications (separate ladder).
- Shortcuts for browser rows (saved rows only).
- Queueing forwarded connects while busy (ignored by design).
- Exit-on-success choreography for shortcut connects (launcher stays open).
- Smart-skip of the overwrite confirmation by reading the existing shortcut (always confirm).
- Per-server auto-launch flags (superseded — a desktop shortcut *is* per-server auto-launch).
- Sweeping orphaned icon files.
- Auto-updating existing shortcuts when a server changes (re-create on demand).
- IPC beyond the single forwarded connect line (no richer protocol).

## Further Notes

- Design closed through four grilling rounds (2026-10-02); every branch visited, nothing
  silently assumed. Key reversals worth remembering: the earlier "launcher icon only" call
  was overturned (server icon + fallback), and the earlier "no IPC" v1 recommendation was
  overturned (forwarding built now).
- Suggested ticket order (for to-tickets): startup `--connect` connect → shortcut creation
  (payload + creator seam + confirm) → icon materialization → row menu + delete confirm →
  IPC forwarding. Each is a vertical slice.
- The feature delivers the earlier "per-server auto-launch" idea that was cut as YAGNI:
  explicit shortcuts are the better answer to "always connect to this one".
