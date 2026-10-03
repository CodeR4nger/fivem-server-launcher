# 01: Startup `--connect`: launch straight into a connect

**What to build:** Running the launcher with `--connect <address>` opens the window and
performs the full validated connect pipeline for that address — saved-server requirement
merge, external-app prep, launch — without anyone pressing ENTER SERVER. Same shape as the
auto-launch flow, but for an explicitly supplied address.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] `--connect <address>` fills the address field and runs the connect in-line on startup,
     showing the same live status the button flow shows (resolving / starting app / launch
     result)
- [ ] The connect is identical to a manual connect: saved-server requirements are merged,
     required apps are started and waited on until fully ready, the launch result is reported
     through the existing status mapping
- [ ] An explicit `--connect` address suppresses the auto-launch block; without the arg,
     auto-launch behaves exactly as today
- [ ] A successful connect persists the address as the last-server memory; a failure never
     clobbers it
- [ ] Invalid or stale address → the existing "Invalid address" status; the window stays open
     on success and failure (no exit choreography)
- [ ] Repeated flag → last wins; unknown args ignored; zero validation at parse time (the
     connect pipeline owns validation)
- [ ] The startup update check still runs afterwards
- [ ] View-model tests cover: connect with the arg, auto-launch precedence without it,
     invalid address, persistence rules — through the existing fakes at the view-model seam
