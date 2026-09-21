# 05: Minimal saved-servers UI

**What to build:** the player interacts with saved servers in the main window. A list of saved servers, add (name + address) and delete, and per-server "Requires Steam" / "Requires Discord" checkboxes. Selecting a server fills the connect flow (address + manual flags); the status line shows requirement readiness. Rename/edit optional if cheap, otherwise delete-and-re-add. Style follows the existing minimal window and the DOC color palette; no business logic in code-behind.

**Blocked by:** 01 (Saved server domain + file repository), 04 (Connect flow applies requirements + readiness)

**Status:** ready-for-agent

- [ ] Saved servers appear in the main window list from the repository.
- [ ] Add / delete (and rename) a saved server via the UI, persisted.
- [ ] Per-server Steam/Discord requirement checkboxes persist with the server.
- [ ] Selecting a saved server populates the connect flow; readiness shows in the status line.
- [ ] Manual end-to-end check passes (save → restart launcher → still there → connect).
- [ ] Suite green.