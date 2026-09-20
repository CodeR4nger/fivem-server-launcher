# 01: Alinear clasificación de dominios + contrato de excepción con la spec

**What to build:** dos fixes decididos en la enmienda del spec:
1. `ServerAddress.IsValidDomain` acepta **1+ labels** (prop: `localhost:30120` → `DomainPort`). La regla "TLD con letra" aplica solo con 2+ labels, evitando que una IP de 4 octetos con octeto inválido (`999.56.120.52:30320`) se clasifique como dominio.
2. `ServerResolver` construye `InvalidAddressException` siempre con la **dirección original** que ingresó el usuario (hoy pasa el `cfxId` extraído cuando CFX devuelve null).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `localhost:30120` (y cualquier 1-label válido) → `ServerAddressKind.DomainPort`.
- [x] `999.56.120.52:30320` (IP de 4 octetos con octeto inválido) sigue → `Unknown` (sin regresión del fix TLD de la fase previa).
- [x] Regla "TLD con letra" documentada en `spec.md` de la fase actual (ya está en `Implementation Decisions`; verificar que quedó como decisión escrita, no solo en comentario de ticket).
- [x] `InvalidAddressException` en el resolver usa la dirección original en los 3 caminos: whitespace/vacío, forma `Unknown`, y CFX null.
- [x] Tests nuevos en `ServerAddressTests` (localhost → DomainPort; IP inválida → Unknown) y `ServerResolverTests` (excepción con dirección original).
- [x] Suite completa verde.

## Comments