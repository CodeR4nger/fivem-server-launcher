# 02: Field titles in the add/edit server dialog

**What to build:** the add/edit server overlay dialog shows a small caption above each of its
two text boxes — NAME above the name box, ADDRESS above the address box — in the same style
family as existing section headers. No ViewModel surface changes.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

NAME / ADDRESS captions added above the dialog TextBoxes using the existing
`SectionHeaderStyle`. Build green; VM untouched.

- [ ] NAME caption above the name TextBox
- [ ] ADDRESS caption above the address TextBox
- [ ] Captions styled consistently with existing headers; dialog layout otherwise unchanged
- [ ] Build green; visual verification

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9b)
