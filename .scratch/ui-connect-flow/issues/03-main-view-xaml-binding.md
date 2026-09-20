# 03: MainView extensión con TextBox de dirección y binding a MainViewModel

**What to build:** wiring XAML. Nuevo layout en `MainView.xaml`: TextBox + botón "Entrar al servidor" + estado textblock. Sin lógica en code-behind. El DataContext no se conecta aquí (se arma en una fase posterior con el composition real del grafo).

**Blocked by:** 02

**Status:** resolved

- [x] TextBox que edita `ServerAddress` (`UpdateSourceTrigger=PropertyChanged`, two-way).
- [x] "ENTRAR AL SERVIDOR" con `Command="{Binding ConnectCommand}"`, disabled cuando `IsBusy`.
- [x] TextBlock con `Text="{Binding StatusText}"`.
- [x] Binding solo; sin concrete `DataContext` — la compo real del grafo es fase posterior.
- [x] Build + suite verde.

## Comments