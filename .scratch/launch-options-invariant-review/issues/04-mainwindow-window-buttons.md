# 04: Window buttons in MainWindow.xaml with shared Style (DRY)

**What to build:** In `src/FiveMServerLauncher/MainWindow.xaml`, the Minimize/Close buttons share a single `Style` (same Width/Height, transparent Background, white Foreground, BorderThickness 0, Hand Cursor, `WindowChrome.IsHitTestVisibleInChrome`), and the repeated layout dimensions (`CaptionHeight="40"`, `RowDefinition Height="40"`, `Width="50"`) become reused static numeric/`GridLength` resources. No behavior change: same values and appearance, same Click handlers.

**Blocked by:** 01

**Status:** resolved

- [x] Minimize and Close use the same shared `Style` (they only differ in `Content` and `Click`/FontSize if applicable)
- [x] `Height="40"`/`Width="50"` are no longer repeated inline across the file; they are referenced from common resources
- [x] `dotnet build FiveMServerLauncher.slnx` green
- [x] Full suite green

## Comments
- Resources `CaptionHeight` (sys:Double for WindowChrome and buttons), `WindowButtonWidth` and `CaptionRowHeight` (GridLength for the RowDefinition, since `RowDefinition.Height` does not accept `Double` via StaticResource at runtime) were added. The `WindowButtonStyle` Style centralizes Width/Height/Background/Foreground/BorderThickness/Cursor/IsHitTestVisibleInChrome; the buttons only declare Content, FontSize and Click.
- The app was briefly launched (`dotnet run --no-build`) and started with no runtime XAML errors.
