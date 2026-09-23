# 01: Compact connect-address area + taller saved-servers list

**What to build:** the connect-address TextBox becomes visually compact (smaller font,
tighter padding, lower minimum height) and the dead vertical space between the "CONNECT
ADDRESS" header, the box, and the ENTER SERVER button is removed; the saved-servers ListBox
MaxHeight grows by the reclaimed space so more servers are visible without resizing the fixed
860×520 window. Layout structure (ScrollViewer/StackPanel) is unchanged.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

`Views/MainView.xaml`: address TextBox compacted (FontSize 12, padding 12,6, MinHeight 32,
tighter margins), CONNECT ADDRESS header margin 20→14, status margin tightened, ENTER SERVER
48px with slimmer margins; ListBox MaxHeight 110→220. Window unchanged (860×520). Build +
suite green.

- [ ] Address text is visibly smaller and the box is shorter
- [ ] Gap between address box and ENTER SERVER button is visibly reduced
- [ ] ListBox shows more rows than before (MaxHeight raised from 110 by the reclaimed space)
- [ ] Right column does not scroll in the common case; window size unchanged
- [ ] Build green; visual verification

**Spec:** `.scratch/v1-1-polish-and-fixes/spec.md` (phase 9, bullet 9a)
