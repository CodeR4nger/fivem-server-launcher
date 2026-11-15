# 07: Server and confirm dialogs as modals

**What to build:** the add/edit server dialog and the confirm dialog in the mockup's modal
style: dimmed backdrop, centred card with title and close ✕, Name and Address fields, Requires
Steam / Requires Discord switches, inline validation error, Cancel and amber Save. The loopback
local-connection parameters (cfx id, game, build, pure mode) become a section inside the
dialog shown only for loopback addresses, replacing the side panel. Confirmations (delete,
shortcut overwrite) use the same modal with Cancel and a confirm button. Esc cancels, Enter
submits, focus moves into the dialog on open, stays trapped while open, and returns to where it
was on close. Backdrop fades and the card pops in. View-model contracts are unchanged.

See `../spec.md` (Dialogs).

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] Every existing dialog/confirm view-model test passes unchanged.
- [ ] Loopback section appears and disappears with the address; its values save as today.
- [ ] Esc/Enter/focus behaviour verified manually and recorded (thin glue, no logic in code-behind).
- [ ] The old side panel and old overlay markup are removed.
- [ ] Layout test for the dialog (with and without the loopback section) in all 11 languages; zero warnings.
