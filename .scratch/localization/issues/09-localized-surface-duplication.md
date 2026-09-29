# 09: Localized-surface duplication cleanups

**What to build:** Three small duplication/primitive-obsession items the code review flagged as
judgement calls. None is a defect; group them or split them as suits.

1. **Brand names re-spelled outside their owner.** `MainViewModel.DialogGameClientOptions` builds
   `new(GameClient.FiveM, "FiveM")`, `new(GameClient.FiveMEnhanced, "FiveM Enhanced")` by hand, while
   `InstalledClientOption.DisplayNameOf` is the single owner of those strings (already used by
   `CfxStatusItem.DisplayName`, `SavedServerItem.GameTagLabel`, `DevClientLabel` and the
   `StatusOpeningClient` / `StatusNotInstalled` formats). Point the dialog options at the owner.
   Relevant because these names are passed as interpolation parameters into localized templates — a
   brand, correctly never translated, and it should be defined once.
2. **Three near-identical option records with inconsistent field order:**
   `GameClientOption(Game, Label)`, `LanguageSettingOption(Tag, Label)`, `GameFilterOption(Label, Game)`.
   Extract one shape, or leave them alone if three two-field records beat an abstraction.
3. **Primitive obsession on the language tag.** `LanguageSettingOption.Tag` and
   `LauncherSettings.Language` are bare `string?` compared with `==`. A small value type would make
   "not a shipped tag" explicit at compile time rather than a runtime lookup miss. Weigh against
   `ServerAddress`'s existing precedent for owning its own identifier form.

**Blocked by:** None.

**Status:** done (item 1 fixed; items 2-3 explicitly accepted as-is)

**Severity:** low

- [x] Items 1-3 triaged individually: fixed or explicitly accepted as-is

## Resolution

1. **Fixed.** `MainViewModel.DialogGameClientOptions` now reads its three brand labels from
   `InstalledClientOption.DisplayNameOf`, the single owner. Pinned by
   `DialogGameClientOptions_ShouldLabelClientsWithTheirBrandNames`; the pre-existing
   `DialogGameClientOptions_WhenLanguageSwitched_ShouldLocalizeNone` no longer re-spells a brand.
2. **Accepted as-is.** The three records are two-field projections of different shapes (a
   `GameClient?`, a language `string?`, a game filter) used in three unrelated binding contexts.
   A shared record would need a nullable-typed field plus a label and buy no safety, so three small
   records stay the cheaper and clearer choice.
3. **Accepted as-is.** A language-tag value type would duplicate what `ServerAddress` already
   does for addresses, but the tag is data read straight from a file name and already validated at
   the point it matters (`SetLanguage` against `ShippedLanguageOptions`). `UnshippedLanguageTag`
   in the test suite shows the "not shipped" case is already handled explicitly where it is tested.
