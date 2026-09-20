# 02: Extraer split host:port compartida (DRY)

**What to build:** la split "ultimo `:` separa host de port" aparece en 3 lugares con forma ligeramente distinta. Extraer un acceso común para que `ServerAddress.IsIpPort`/`IsDomainPort` y `ServerResolver.GetHost`/`GetPort` la compartan, quitando la duplicación sin cambiar comportamiento.

**Blocked by:** 01 (ambos tocan `ServerAddress`, mejor en orden)

**Status:** resolved

- [x] Un solo lugar (helper/metodo en `ServerAddress` o un tipo par host/port) hace la split de `host:port`, usado por la clasificación (`IsIpPort`/`IsDomainPort`) y por `ServerResolver` (reemplaza `GetHost`/`GetPort`).
- [x] Sin cambio de comportamiento observable: todas las clasificaciones y resoluciones actuales siguen igual.
- [x] Suite completa verde (los tests de la fase previa son la red de seguridad del refactor).

## Comments