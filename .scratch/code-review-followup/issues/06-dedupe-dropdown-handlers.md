# 06: Deduplicate dropdown handlers in MainView.xaml.cs

**What to build:** `FiveMOption_Click` and `FiveMEnhancedOption_Click` are identical except for the label they write to the main button. They are replaced by a single handler that receives the label via `CommandParameter` in the XAML.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] A single onClick handler for the dropdown options
- [x] The `FiveMButton` button shows the correct label for the chosen option
- [x] The dropdown collapses and the arrow returns to ▼ after choosing
- [x] MainView.xaml passes the label as `CommandParameter`
