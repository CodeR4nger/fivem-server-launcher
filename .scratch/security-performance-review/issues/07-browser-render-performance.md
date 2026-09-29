# 07: Browser search and image-render performance

**What to build:** Typing in the browser search refreshes the whole 30k-row CollectionView per
keystroke on the UI thread, `MarkSavedRows` is O(rows x saved) per load, the icon converter
allocates a fresh BitmapImage per binding evaluation, and server-published icons decode without
a pixel cap (decompression-bomb memory risk). Debounce the filter, index saved rows, cache
rendered images, and decode at a bounded size.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Search filtering debounces; typing stays smooth on the 30k list
- [x] Saved-row marking is keyed (set lookup), not a per-row scan
- [x] Icons decode at a bounded pixel size; repeated bindings reuse the decoded image

## Landed decisions (recorded per the spec)

- Debounce seam justification (spec requires it for new seams): VM-level idle window with
  injectable `searchDebounce` (default 250 ms) + `searchDelay` func, following the established
  `MainViewModel.RunEnrichmentLoopAsync(cadence, delay)` precedent; `TimeProvider.CreateTimer`
  was rejected because the repo's fake-time pattern is the injectable delay. Rapid keystrokes
  cancel the pending refresh and coalesce into the final text; discrete filters (game/hide)
  stay immediate; a throwing refresh is contained inside the fire-and-forget continuation.
- Decode bound covers BOTH axes (`Views/BoundedIconDecode`, WPF min-ratio scaling): the review
  caught that a width-only clamp lets a 1x(2^31-1) bomb PNG decode ~8.6 GB of pixels — covered
  by a dedicated regression test. PNGs within the 96 px cap decode naturally; unparseable input
  decodes at 96x96.
- The decoded-image cache is a bounded LRU (512 decodes, keyed by the byte[] instances the
  enrichment service reuses), not an unbounded ConditionalWeakTable — rows live until the next
  `LoadAsync`, so a weak-keyed cache would retain one decoded image per ever-realized row.
  The LRU mechanics were extracted into the shared `Service/LruCache`, and the enrichment
  service's icon-byte cache now rides the same class.
- Keyed `IsSaved` marking is bit-identical to the old scan: `MatchesAddress` is plain
  OrdinalIgnoreCase equality (set uses the same comparer) and cfx-id comparison stays
  case-sensitive, exactly as before.
- Waived: non-PNG input below the cap upscales in the fallback path (unreachable — icons are
  PNG); cancelled debounce CTSs are not disposed (harmless; `Task.Delay` owns the timer).
- Post-landing fix: the converter's capacity ctor initially made it XAML-uninstantiable
  (all-optional-parameter ctors compile but BAML requires a true parameterless ctor, and the
  app crashed on start); restored the parameterless ctor and pinned the contract with an
  `Activator.CreateInstance` test that mirrors XAML's default-ctor reflection lookup.
