# 03: Button styles in MainView.xaml via BasedOn (DRY)

**What to build:** In `src/FiveMServerLauncher/Views/MainView.xaml`, `DropdownButtonStyle` and `ArrowButtonStyle` inherit from a common base style with `BasedOn` (the pattern is already demonstrated by `SecondaryButtonStyle` over `MainButtonStyle`), declaring only their differences (background, border, alignment, padding, cursor, template where applicable). The re-declaration of the ~9 shared setters is removed. No visual behavior change: same values, same appearance.

**Blocked by:** 01

**Status:** resolved

- [x] `DropdownButtonStyle` and `ArrowButtonStyle` use `BasedOn` instead of repeating shared setters
- [x] Each style's values (Background, Foreground, BorderBrush, BorderThickness, FontSize, FontWeight, alignments, Padding, Cursor, Template) remain visually identical to today after the refactor
- [x] `dotnet build FiveMServerLauncher.slnx` green
- [x] Full suite green

## Comments
- `BaseButtonStyle` was introduced with the common-value setters (Background PanelLight, Foreground Text, BorderBrush Border, BorderThickness 1, Cursor Hand, VAlign Center, Template). `MainButtonStyle` inherits and only declares FontSize/FontWeight/HAlign/Padding; `DropdownButtonStyle` inherits from base and overrides BorderThickness/FontSize/FontWeight/HAlign/Padding; `ArrowButtonStyle` inherits from base and only sets HAlign=Center. Secondary keeps `BasedOn` Main with a Background override. It was verified value-by-value that each style ended up identical to the original (Arrow had no FontSize/Padding, which is why it doesn't inherit from Main).
