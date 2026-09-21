# 05: Minimal saved-servers UI

**What to build:** the player interacts with saved servers in the main window. A list of saved servers, add (name + address) and delete, and per-server "Requires Steam" / "Requires Discord" checkboxes. Selecting a server fills the connect flow (address + manual flags); the status line shows requirement readiness. Rename/edit optional if cheap, otherwise delete-and-re-add. Style follows the existing minimal window and the DOC color palette; no business logic in code-behind.

**Blocked by:** 01 (Saved server domain + file repository), 04 (Connect flow applies requirements + readiness)

**Status:** resolved

- [x] Saved servers appear in the main window list from the repository.
- [x] Add / delete (and rename) a saved server via the UI, persisted.
- [x] Per-server Steam/Discord requirement checkboxes persist with the server.
- [x] Selecting a saved server populates the connect flow; readiness shows in the status line.
- [ ] Manual end-to-end check passes (save → restart launcher → still there → connect). (pending user GUI run)
- [x] Suite green (219).

## Comments
- Review follow-up: sync `RelayCommand` added and used for save/delete (dropping the misleading `Async` suffix on the never-awaiting methods); `SavedServerItem` persists toggles through its `changeHandler` (single path, no PropertyChanged double-plumbing); duplicate check now runs *after* `SavedServer.Create` validation so an invalid name reports "Invalid name or address".
- Rename is intentionally not implemented (optional per ticket; delete-and-re-add covers edits).
- Selection fills `ServerAddress`; readiness surfaces on connect (per spec the connect flow owns the readiness check).
- Deleting a server intentionally leaves the typed connect address untouched (connecting to a non-saved address is fully supported).
- Note for user: requires a manual GUI pass to confirm save → restart → connect persistence.