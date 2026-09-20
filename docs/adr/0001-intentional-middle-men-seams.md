# ADR-0001: Seams intencionales (ConfigurationRepository y ServerRequirementsResolver)

**Status:** Accepted

## Context

Dos clases delegan mayormente en sus dependencias y podrían leerse como “middle men”:

- `ConfigurationRepository` envuelve `ISettingsStorage`: `Save` añade un null-guard y `Load` aplica `?? new LauncherSettings()`.
- `ServerRequirementsResolver` mapea `CfxServerInfo` → `ServerRequirements` en tres líneas.

La revisión de código (`.scratch/code-review-followup/issues/08-middle-men-decision.md`) pedía decidir entre justificarlas como seams intencionales o inlinearlas en sus consumidores.

## Decision

Se mantienen ambas como seams intencionales, sin código de producción adicional (no se tocan hasta que haya un consumidor real de la UI).

- `ConfigurationRepository` es la API no-nula del módulo `Configuration`. Su frontera: aísla a los consumidores (UI/VM) de la nulabilidad del storage y concentra los guards de entrada. Sustituible en tests por `InMemorySettingsStorage` sin tocar filesystem.
- `ServerRequirementsResolver` traduce el shape de CFX (`CfxServerInfo`, propiedad del Service layer) a tipos propios del Domain (`ServerRequirements`). Mantiene el Domain libre de las convenciones de nombres de CFX (`sv_enforceGameBuild`, `sv_pureLevel`, `requestSteamTicket`) y es testeable directo, sin HTTP.

No se inlinean: acoplar Domain o UI al shape de CFX o a la nulabilidad del storage contradice la regla de mantener la lógica de negocio fuera de la UI (ver `AGENTS.md`) y degrada la testabilidad.

## Consequences

- El código actual no cambia de comportamiento; la suite sigue verde.
- `ServerRequirementsResolver` ya es una pieza del flujo `ServerResolver` (see `ServerResolver.ResolveAsync`).
- `ConfigurationRepository` no tiene consumidor en producción todavía; se documenta su papel para cuando la UI se conecte.
- Futuras revisiones deben justificar romper estos seams antes de hacerlo.