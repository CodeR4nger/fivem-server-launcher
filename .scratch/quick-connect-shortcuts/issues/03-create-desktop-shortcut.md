# 03: Create desktop shortcut (launcher icon)

**What to build:** The full end-to-end story: from the row menu, create a desktop shortcut
for a saved server. Double-clicking the shortcut runs the launcher with the connect address
and connects through the whole pipeline (delivered by ticket 01). This ticket uses the
launcher icon; server icons arrive in ticket 04.

**Blocked by:** 01 (the `--connect` entry the shortcut embeds), 02 (the row menu that
triggers it, and the confirm overlay it reuses).

**Status:** ready-for-agent

- [x] The menu gains "Create desktop shortcut"; acting on a row writes
     `<sanitized server name>.lnk` to the desktop
- [x] Payload: the raw saved address embedded as `--connect "<address>"`; target is the
     running exe path (never the single-file extraction dir)
- [x] Server names are sanitized for filename validity; a same-name collision opens the
     confirm overlay ("A shortcut named X already exists. Overwrite?") and overwrites on
     confirm — re-creating after an edit is the update mechanism
- [x] Shortcuts use the launcher icon in this ticket
- [x] Success → "Shortcut created" status; any failure (unwritable desktop, creator error) →
     "Could not create shortcut"; never a crash, never a modal
- [x] Creation is not gated by the busy flag — it is independent of the connect pipeline
- [x] A new shortcut-creator seam (create shortcut at a path with target, arguments and
     optional icon path) — fakes record calls in tests; the real WScript.Shell COM
     implementation (the app's first COM interop; dynamic dispatch, trimming off so the
     single-file build is safe) is thin untested glue; an IShellLink P/Invoke implementation
     is the documented plan B
- [x] The desktop path arrives through an injectable function provider (the established
     portable-directory pattern)
- [x] New strings ship in all 11 languages; the completeness audit test stays green
