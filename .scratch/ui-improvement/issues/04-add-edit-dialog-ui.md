# 04: Add/Edit dialog XAML + row edit buttons

**What to build:** the dialog overlay renders centered on top of the app panel when
`IsServerDialogOpen`, with Name/Address/Steam/Discord fields + SAVE/CANCEL buttons; the
saved-servers list rows gain a small ✏ Edit button (only on the selected row, accent colour on
hover, flush right) commanding `OpenEditServerDialogCommand`; the old "add row" at the bottom of
the list is removed, replaced by a `+ NEW SERVER` button below the list (user approved placement
over the header). Window icon wired to `Assets/launcher.ico` (multi-size, `ApplicationIcon`).

**Blocked by:** 03.

**Status:** resolved
