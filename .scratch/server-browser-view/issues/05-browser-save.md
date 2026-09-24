# 05: Save from the browser

**What to build:** a Save action on a browser row that opens the existing add/edit server
dialog on the main view, prefilled with the server's display name and its address (catalog
endpoint; cfx-join form for hidden servers). The phase-9 save flow applies unchanged
(validation, duplicate check, immediate refresh, background CfxId capture). Rows whose server
is already saved (by cfx id or address) show a saved marker.

**Blocked by:** 02 (ServerBrowserViewModel with filters).

**Status:** resolved

## Answer

`ServerBrowserItem.IsSaved` (+`SetSaved`) marked by `ServerBrowserViewModel.MarkSavedRows` on
each load (cfx-id or address match against the repository). Browser Save opens the existing
add dialog via `MainViewModel.OpenSaveServerDialog(name, address)` (same validation path;
duplicates impossible by repository rejection). Suite green.

- [ ] Save opens the existing dialog prefilled with name + address
- [ ] Hidden-sentinel rows prefill the cfx-join form
- [ ] Already-saved rows are visually marked
- [ ] Duplicate attempts are impossible (repository rejects / dialog shows existing state)
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/server-browser-view/spec.md`
