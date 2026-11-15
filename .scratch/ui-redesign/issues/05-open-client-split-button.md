# 05: Open client split button with one preferred-client setting

**What to build:** the bottom bar's open-client control. Its main part reads "Open <preferred
client>" and opens it (same launch path and status messages as today's OPEN). Its ▾ part lists
installed clients with the current one marked; picking one saves it as the Preferred client and
launches nothing. The separate "selected for this session" client is gone: the button and the
Settings Preferred client control read and write the same state, so changing either updates
the other immediately. A preferred client that is no longer installed falls back to the first
installed one; with no client installed the button reads "No client installed" and is disabled.
Secondary styling (Connect stays the only amber primary on the Servers page); no CFX status dot
on it; no right-click behaviour.

See `../spec.md` (Open client button).

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] One view-model state backs both editors; tests prove a change from either side is seen by the other and persisted once.
- [ ] Save failure reverts the selection with the existing save-failed status (persistence convention).
- [ ] Not-installed fallback and no-client disabled state tested.
- [ ] Dropdown opens/closes from mouse and keyboard, Esc closes it, focus returns to the button.
- [ ] Old OPEN split-button markup and its code-behind toggle are removed; tests that named the old selection are rewritten under new names.
- [ ] Layout test for the bottom bar in all 11 languages (long client names do not clip); zero warnings.
