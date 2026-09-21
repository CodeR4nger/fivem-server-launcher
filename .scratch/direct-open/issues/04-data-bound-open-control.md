# 04: Data-bound OPEN control

**What to build:** the OPEN dropdown and button become data-bound to the view model: the list shows
only the installed clients from the VM, the button content follows the selected client, the arrow
toggles the list, and pressing the button invokes the open command. The label-swapping code-behind
handler is removed; only the pure-visual show/hide toggle stays in the view.

**Blocked by:** 03 (VM open command and installed-client list).

**Status:** resolved

- [x] The OPEN control renders the installed-client list from the view model.
- [x] The button label follows the selected client from the view model.
- [x] Pressing the button and selecting a client from the list drive the view-model command and
      selection.
- [x] The label-swapping code-behind logic is gone; no business logic lives in the view.