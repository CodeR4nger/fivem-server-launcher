# 04: Connect-flow wiring

**What to build:** The connect flow primes the client config before launch: `GameLauncher.ConnectAsync` awaits the preparer on the resolved profile as a pre-launch step, with the `LaunchResult` contract (`Connect`/`OpenClient`/`StartFailed`) exactly unchanged, and the composition root wires the new seams in. Launch documentation (AGENTS.md) describes the priming step.

**Blocked by:** 03 (CitizenFX.ini preparer decision).

**Status:** done

- [x] `GameLauncher.ConnectAsync` primes via the preparer before building launch options / starting the process; primming runs even for best-effort no-ops.
- [x] `LaunchResult` outcome identical whether priming wrote, skipped, or (via the fake) was never reached — a priming failure can never surface as a failed connect.
- [x] Composition root constructs the preparer from the real locator and writer and injects it into `GameLauncher`.
- [x] Tested at `GameLauncher` with a recording fake preparer; AGENTS.md Launch/Service sections mention the priming step and the new seams.