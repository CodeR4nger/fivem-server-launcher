# 05: Deduplicar templates de botón en MainView.xaml

**What to build:** los tres estilos de botón de `MainView.xaml` (MainButton, DropdownButton, ArrowButton) repiten el mismo template Border/ContentPresenter y los triggers HOVER/PRESSED. Se extrae un template compartido parametrizado con `TemplateBinding` a los brushes; cada estilo solo personaliza colores/paddings.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Un único `ControlTemplate` compartido por los tres estilos
- [ ] El comportamiento visual (hover/pressed/disabled) se conserva exactamente
- [ ] Sin cambios de diseño visibles (solo estructura)
- [ ] La app compila (build WPF OK)