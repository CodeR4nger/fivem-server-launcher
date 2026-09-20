# 04: Fixture de archivo temporal en FileSettingsStorageTests

**What to build:** elimina la duplicación de `FileSettingsStorageTests` extrayendo un fixture reutilizable que crea un directorio temporal (y lo limpia) para cada test. También se reutiliza el caso de guardado/lectura completo en vez de repetirlo entre `SaveAndLoad` y `Load`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Un helper/fixture proporciona un path temporal único por test y lo elimina en teardown
- [ ] Los 9 usos del patrón temp-dir + `try/finally Delete` lo consumen
- [ ] La duplicación SaveAndLoad↔Load se reduce compartiendo el escenario
- [ ] Suite verde sin regresiones