# 03: VM open command and installed-client list

**What to build:** the view model exposes an open action and a dropdown data source that only lists
the installed clients. The user sees their installed clients (FiveM / FiveM Enhanced), the open
button reflects the selected one, pressing it launches that client, and the status line reports
outcomes ("Opening {client}...", or a clear English message when the client is no longer found).
The busy guard applies so repeated presses cannot double-launch. Calling the connect flow that ends
in "open client" surfaces the same status vocabulary.

**Blocked by:** 01 (open action at the Application seam).

**Status:** resolved

- [x] The open command launches the selected client through the launcher and maps the outcome to
      status text.
- [x] The dropdown source contains only installed clients, ordered Legacy first, with display names
      (FiveM / FiveM Enhanced).
- [x] The selected client drives the open button label; changing the selection changes the label.
- [x] The open command is disabled while busy or when no client is selected.