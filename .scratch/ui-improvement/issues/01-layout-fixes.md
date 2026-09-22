# 01: Fix layout clipping + mutually exclusive panels

**What to build:** stop text from being trimmed in the two header buttons and the SPLIT button area,
let the Dev Mode panel size to content (row auto), make settings/dev mutually exclusive (opening
one hides the other), give the saved-servers list breathing room (row padding + explicit margin so
row 0 is fully visible), and give the connect box explicit safe height+padding.

**Blocked by:** none.

**Status:** resolved

- [x] Header buttons sized to fit (auto height or adequate height + padding).
- [x] Dev panel bottom visible in all states.
- [x] Opening SETTINGS closes Dev Mode panel; opening DEV MODE closes Settings.
- [x] First row of the saved-servers list fully readable.
- [x] Connect address box not clipped.
