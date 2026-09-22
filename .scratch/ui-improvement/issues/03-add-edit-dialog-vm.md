# 03: Add/Edit server dialog VM

**What to build:** VM state + commands for the add/edit dialog: `IsServerDialogOpen`,
`EditingServer`, `DialogServerName` / `DialogServerAddress` / `DialogRequiresSteam` /
`DialogRequiresDiscord`, `OpenAddServerDialogCommand`, `OpenEditServerDialogCommand`,
`SaveServerDialogCommand`, `CancelServerDialogCommand`. Save uses `Add` for a new server, `Update`
for editing with the same address (case-insensitive), remove + add when the address changes.
Validation failures surface as `DialogError` inside the dialog: "Invalid name or address" /
"Server already saved".

**Blocked by:** none.

**Status:** resolved

- [x] Open-add starts a fresh dialog; open-edit pre-fills from the row's server.
- [x] Save on add persists the server and adds it to the list; cancels before save drop the dialog.
- [x] Save on edit with same address → Update; with changed address → Remove + Add.
- [x] Cancel always just closes the dialog.
