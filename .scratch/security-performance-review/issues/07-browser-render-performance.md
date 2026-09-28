# 07: Browser search and image-render performance

**What to build:** Typing in the browser search refreshes the whole 30k-row CollectionView per
keystroke on the UI thread, `MarkSavedRows` is O(rows x saved) per load, the icon converter
allocates a fresh BitmapImage per binding evaluation, and server-published icons decode without a
pixel cap (decompression-bomb memory risk). Debounce the filter, index saved rows, cache rendered
images, and decode at a bounded size.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Search filtering debounces; typing stays smooth on the 30k list
- [ ] Saved-row marking is keyed (set lookup), not a per-row scan
- [ ] Icons decode at a bounded pixel size; repeated bindings reuse the decoded image
