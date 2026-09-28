# 08: Process probe handle hygiene

**What to build:** The readiness probe calls `Process.GetProcessesByName` and never disposes the
returned array, leaking native handles per poll (two snapshots plus a registry read every cycle
while preparing an external app). Dispose the probe results at the injectable lookup seam.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] The default process lookup disposes every returned process instance
- [ ] Fakes and test behavior unchanged
