# 02: Direct tests for the four legacy data-only converters

**Type:** feature

**What to build:** direct converter tests mirroring `CfxStatusToBrushConverterTests`
(precedent set in the cfx-status phase) for:

- `GameClientToBrushConverter` — FiveM→orange, FiveMEnhanced→blue, RedM→red,
  unmapped/default→grey, `ConvertBack` throws `NotSupportedException`.
- `InverseBoolToVisibilityConverter` — `true`→Collapsed, `false`→Visible,
  non-bool value→Visible, `ConvertBack` round-trips Collapsed/Visible.
- `SelectionEqualityToVisibilityConverter` — equal (ReferenceEquals) → Visible,
  different → Collapsed, `ConvertBack` throws.
- `BytesToImageSourceConverter` — null→null, empty array→null, non-`byte[]`
  value→null, valid PNG bytes→frozen `BitmapImage`, `ConvertBack` throws.

**Blocked by:** 01.

**Acceptance:** each test asserts only the converter's public `Convert`/
`ConvertBack` contract; fixture bytes (tiny valid PNG) is a local array.

**Status:** resolved

- [x] RED: characterizations written against the public converter contracts
- [x] GREEN: 22 tests pass (5 + 6 + 5 + 6)
- [x] REFACTOR: none needed (data-only converters)