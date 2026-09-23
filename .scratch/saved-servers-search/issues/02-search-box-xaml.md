# 02: Search box above the saved-servers list

**What to build:** an always-visible search TextBox directly above the saved-servers ListBox
(below the header row with the refresh button), showing a subtle "Search servers" placeholder
when empty, bound to `ServerSearchText` with immediate (property-changed) updates. Styling
consistent with the dialog inputs (panel fill, border, 12px). XAML only binds; no code-behind.

**Blocked by:** 01 (Search filtering on MainViewModel).

**Status:** ready-for-agent

- [ ] Search box visible above the list at all times (no toggle)
- [ ] "Search servers" placeholder shown when empty
- [ ] Typing filters the list live, clearing restores it
- [ ] Build green; visual verification by the user

**Spec:** `.scratch/saved-servers-search/spec.md`
