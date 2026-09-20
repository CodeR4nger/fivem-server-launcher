# 03: MainView extension with address TextBox and binding to MainViewModel

**What to build:** XAML wiring. New layout in `MainView.xaml`: TextBox + "Entrar al servidor" button + status textblock. No logic in code-behind. The DataContext is not wired here (it is assembled in a later phase with the real graph composition).

**Blocked by:** 02

**Status:** resolved

- [x] TextBox that edits `ServerAddress` (`UpdateSourceTrigger=PropertyChanged`, two-way).
- [x] "ENTRAR AL SERVIDOR" with `Command="{Binding ConnectCommand}"`, disabled when `IsBusy`.
- [x] TextBlock with `Text="{Binding StatusText}"`.
- [x] Binding only; no concrete `DataContext` — the real graph composition is a later phase.
- [x] Build + green suite.

## Comments
