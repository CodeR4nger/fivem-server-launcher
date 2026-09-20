# 03: Tests de Configuration usan Given/When/Then

**What to build:** los tests de la carpeta `Configuration` siguen el convenio de comentarios de AGENTS.md (`Given/When/Then` con naming `<Método>_Should<Expectativa>`), igual que el resto de la suite. No cambia comportamiento ni assertions, solo la estructura de comentarios.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `ConfigurationRepositoryTests` usa Given/When/Then en lugar de Arrange/Act/Assert
- [ ] `FileSettingsStorageTests` usa Given/When/Then
- [ ] `InMemorySettingsStorageTests` usa Given/When/Then
- [ ] `LauncherSettingsTests` añade los comentarios Given/When/Then (hoy sin ellos)
- [ ] Suite completa sigue verde (solo cambio cosmético de comentarios)