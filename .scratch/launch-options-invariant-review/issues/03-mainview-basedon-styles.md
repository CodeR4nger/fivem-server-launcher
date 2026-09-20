# 03: Estilos de botón en MainView.xaml vía BasedOn (DRY)

**What to build:** En `src/FiveMServerLauncher/Views/MainView.xaml`, `DropdownButtonStyle` y `ArrowButtonStyle` heredan de un estilo base común con `BasedOn` (el patrón ya lo demuestra `SecondaryButtonStyle` sobre `MainButtonStyle`), declarando solo sus diferencias (fondo, borde, alineación, padding, cursor, template donde aplique). Se elimina la re-declaración de los ~9 setters compartidos. Sin cambio de comportamiento visual: mismos valores, misma apariencia.

**Blocked by:** 01

**Status:** resolved

- [x] `DropdownButtonStyle` y `ArrowButtonStyle` usan `BasedOn` en lugar de redundar setters compartidos
- [x] Los valores de cada style (Background, Foreground, BorderBrush, BorderThickness, FontSize, FontWeight, alineaciones, Padding, Cursor, Template) quedan idénticos visualmente a hoy tras el refactor
- [x] `dotnet build FiveMServerLauncher.slnx` verde
- [x] Suite completa verde

## Comments
- Se introdujo `BaseButtonStyle` con los setters de valor común (Background PanelLight, Foreground Text, BorderBrush Border, BorderThickness 1, Cursor Hand, VAlign Center, Template). `MainButtonStyle` hereda y solo declara FontSize/FontWeight/HAlign/Padding; `DropdownButtonStyle` hereda de base y overridea BorderThickness/FontSize/FontWeight/HAlign/Padding; `ArrowButtonStyle` hereda de base y solo setea HAlign=Center. Secondary sigue `BasedOn` Main con override de Background. Se verificó valor-a-valor que cada estilo quedó idéntico al original (Arrow no tenía FontSize/Padding, por eso no hereda de Main).