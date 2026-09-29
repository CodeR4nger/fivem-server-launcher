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

**Status:** ready-for-agent

**Severity:** low

- [ ] Items 1-3 triaged individually: fixed or explicitly accepted as-is
