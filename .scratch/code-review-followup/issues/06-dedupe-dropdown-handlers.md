# 06: Deduplicar handlers del dropdown en MainView.xaml.cs

**What to build:** `FiveMOption_Click` y `FiveMEnhancedOption_Click` son idénticos salvo el label que escriben en el botón principal. Se sustituyen por un único handler que recibe el label vía `CommandParameter` en el XAML.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Un solo handler onClick para las opciones del dropdown
- [ ] El botón `FiveMButton` muestra el label correcto según la opción elegida
- [ ] El dropdown colapsa y la flecha vuelve a ▼ tras elegir
- [ ] MainView.xaml pasa el label como `CommandParameter`