# 04: Botones de ventana en MainWindow.xaml con Style compartido (DRY)

**What to build:** En `src/FiveMServerLauncher/MainWindow.xaml`, los botones Minimizar/Cerrar comparten un `Style` único (mismo Width/Height, Background transparent, Foreground blanco, BorderThickness 0, Cursor Hand, `WindowChrome.IsHitTestVisibleInChrome`), y las dimensiones repetidas del layout (`CaptionHeight="40"`, `RowDefinition Height="40"`, `Width="50"`) pasan a recursos estáticos numéricos/`GridLength` reutilizados. Sin cambio de comportamiento: mismos valores y apariencia, mismo Click handlers.

**Blocked by:** 01

**Status:** resolved

- [x] Minimizar y Cerrar usan el mismo `Style` compartido (solo difieren `Content` y `Click`/FontSize si aplica)
- [x] `Height="40"`/`Width="50"` no se repiten inline a través del archivo; se referencian desde recursos comunes
- [x] `dotnet build FiveMServerLauncher.slnx` verde
- [x] Suite completa verde

## Comments
- Se añadieron recursos `CaptionHeight` (sys:Double para WindowChrome y botones), `WindowButtonWidth` y `CaptionRowHeight` (GridLength para la RowDefinition, ya que `RowDefinition.Height` no acepta `Double` vía StaticResource en runtime). El Style `WindowButtonStyle` centraliza Width/Height/Background/Foreground/BorderThickness/Cursor/IsHitTestVisibleInChrome; los botones solo declaran Content, FontSize y Click.
- La app se lanzó brevemente (`dotnet run --no-build`) y arrancó sin errores de XAML en runtime.