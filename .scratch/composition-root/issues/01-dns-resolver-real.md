# 01: DnsResolver real (domain:port resolución)

**What to build:** `IDnsResolver` existe pero no tiene implementación de producción. Crear `Service/DnsResolver.cs` usando `System.Net.Dns` para resolver un dominio a IPv4. Sin tests reales (se llama al OS); el seam IDnsResolver ya está testeado via fakes. De gracia: se recibe el host ya solo (sin puerto), split hecho por el caller.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `Service/DnsResolver.cs` con `ResolveToIpAsync(string)` IPv4. Y si no resuelve → null.
- [ ] Compila. Suite verde.

## Comments