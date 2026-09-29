# 08: Process probe handle hygiene

**What to build:** The readiness probe calls `Process.GetProcessesByName` and never disposes the
returned array, leaking native handles per poll (two snapshots plus a registry read every cycle
while preparing an external app). Dispose the probe results at the injectable lookup seam.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] The default process lookup disposes every returned process instance
- [x] Fakes and test behavior unchanged

## Landed decisions (recorded per the spec)

- New seam justification (spec: "a fix requiring a new seam must be justified in its ticket"):
  `Process.Dispose` never calls `Component.Dispose`, so the `Disposed` event cannot observe
  disposal — the existing `Func<string,bool>` lookup seam cannot pin "every instance is
  released" without touching real OS processes (forbidden by the testing rules). The probe
  therefore owns the real defaults behind two symmetric injectable delegates,
  `ProcessProbe.AnyRunning(name, fetch?, release?)` (fetch = `GetProcessesByName`,
  release = `Dispose`), each overridable like every other real-default seam in the repo. The
  checker's public `Func<string,bool>` seam is unchanged; all 12 existing checker tests pass
  untouched.
- Waived: a release that throws mid-loop aborts the remaining disposals — the real default
  (`Process.Dispose` on enumerated instances) has no throwing path, and the checker already
  degrades any lookup exception to `false`; a per-item catch would swallow real faults.
- Hand-off to ticket 09: `ProcessStarter.Start` discards the `Process` returned by
  `Process.Start` (same undisposed-instance class, but one-shot per launch rather than polled).
