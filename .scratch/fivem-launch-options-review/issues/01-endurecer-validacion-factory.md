# 01: Endurecer y blindar la validación de la fábrica

**What to build:** la fábrica `Create` de `FiveMLaunchOptions` rechaza cualquier address degenerada, no solo vacía o sin prefijo: una address con el prefijo `cfx.re/join/` pero sin id, o con espacios internos/trailing, no construye opciones. Además el invariante de validez deja de ser bypasseable: los campos no pueden poblarse esquivando la fábrica (constructor posicional privado o equivalente), de modo que ningún estado inválido se serializa en silencio.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Dado `cfx.re/join/` (prefijo sin id), `Create` lanza `InvalidAddressException`
- [x] Dado `cfx.re/join/y4lg95 ` (trailing whitespace), `Create` lanza `InvalidAddressException`
- [x] Dado `cfx.re/join/y4 lg95` (espacio interno), `Create` lanza `InvalidAddressException`
- [x] Addresses válidas (`cfx.re/join/y4lg95`) siguen construyendo sin error (sin regresión)
- [x] No es posible construir `FiveMLaunchOptions` con estado inválido fuera de la fábrica (bloqueado en diseño/compilación)
- [x] La suite completa sigue verde tras el cambio