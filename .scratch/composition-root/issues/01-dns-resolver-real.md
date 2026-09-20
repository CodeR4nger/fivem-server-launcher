# 01: Real DnsResolver (domain:port resolution)

**What to build:** `IDnsResolver` exists but has no production implementation. Create `Service/DnsResolver.cs` using `System.Net.Dns` to resolve a domain to IPv4. No real tests (it calls the OS); the IDnsResolver seam is already tested via fakes. As a given: it receives the host alone (no port), the split is done by the caller.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `Service/DnsResolver.cs` with `ResolveToIpAsync(string)` IPv4. And if it does not resolve → null.
- [ ] Compiles. Suite green.

## Comments
