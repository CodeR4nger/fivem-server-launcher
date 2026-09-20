# 05: Deduplicate button templates in MainView.xaml

**What to build:** the three button styles in `MainView.xaml` (MainButton, DropdownButton, ArrowButton) repeat the same Border/ContentPresenter template and the HOVER/PRESSED triggers. A shared template is extracted, parameterized with `TemplateBinding` to the brushes; each style only customizes colors/paddings.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] A single `ControlTemplate` shared by the three styles
- [x] Visual behavior (hover/pressed/disabled) is preserved exactly
- [x] No visible design changes (structure only)
- [x] The app compiles (WPF build OK)
