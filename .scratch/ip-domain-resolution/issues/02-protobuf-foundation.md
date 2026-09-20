# 02: Fundación protobuf para el catálogo (schema master.Server + frames)

**What to build:** el repo aprende a decodificar el formato binario del catálogo CFX. Se añade la dependencia `Google.Protobuf` (con codegen en build vía `Grpc.Tools`), se define el schema `.proto` de `master.Server`/`ServerData`/`Player` (verificado contra las librerías comunitarias `cfx-api`/`fivem-server-api`: frame = uint32 LE length + mensaje protobuf) y se entrega un helper que recibe el stream binario y devuelve la secuencia de mensajes `master.Server` decodificados. Es la base de la ticket 03 y la primera dependencia NuGet del proyecto.

**Nota:** `Player` queda **fuera de scope** (spec.md línea 62: "Detallado completo del catálogo (íconos, upvotes, players) ... se mapea solo lo que `ServerResolver` necesita"). El schema solo cubre `Server` + `ServerData`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `Google.Protobuf` + codegen de build integrados en el proyecto principal (sin romper el build).
- [x] Schema `.proto` con `master.Server` (EndPoint + Data) y `ServerData` (vars map string→string, connectEndPoints repeated string, server, clients, svMaxclients, hostname, gametype, mapname...) acorde a lo verificado. `Player` fuera de scope (spec.md:62).
- [x] Helper de parsing: lee uint32 LE length prefix por frame, corta el frame y lo decodifica con el tipo `master.Server`; frames corruptos/truncados se ignoran sin tirar el resto.
- [x] Un frame binario de fixture (construido en el test con el escritor protobuf) decodifica al modelo esperado (EndPoint y campos relevantes de Data).
- [x] Suite existente sigue verde (esta ticket no cambia comportamiento de dominio).
- [x] Naming/tests estilo Given/When/Then; fixtures binarios construidos en el test, no archivos sueltos.

## Comments

- TDD RED→GREEN→REFACTOR completado con 6 tests nuevos en `tests/.../Service/ServerCatalogDecoderTests.cs`. Suite completa 81 verdes (62 previos + 13 de ticket 01 + 6 nuevos).
- `Google.Protobuf 3.36.2` + `Grpc.Tools 2.84.0` (codegen en build) agregados al `.csproj` principal; `Proto/master.proto` registrado vía `<Protobuf>`. Generado: `obj/.../Proto/Master.cs` (namespace `Master`).
- El decoder (`Service/ServerCatalogDecoder.Decode`) tolera: frame truncado al final (bounds-check → corta), frame corrupto entre válidos (catch `InvalidProtocolBufferException` → salta y sigue). Endianness LE asumida explícita (Windows-only).
- Propiedad generada del codegen: `connectEndPoints` → `ConnectEndPoints`; `svMaxclients` → `SvMaxclients`; `gamename`/`sv_enforceGameBuild`/`sv_pureLevel`/`requestSteamTicket` viajan en `vars` (map).
- El schema es "solo lo que el resolver usa" (spec.md:62): sin `Player`, sin campos de íconos/upvotes.