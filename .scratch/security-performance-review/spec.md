# Feature: Security and performance review + improvements (v1.2)

Status: ready-for-agent

## Problem Statement

The launcher grew across v1.0-v1.2 (network services, catalog decoding, process launching,
persistence, background loops, new VMs) without a dedicated security and performance pass. Latent
risks — unbounded network waits on the interactive connect path, untrusted-input parsing, file
write atomicity, shell argument handling — and performance hotspots (sequential per-row network
fetches, per-minute request volume, repeated process scans) can hang or degrade the UI and put
user data at risk. Nothing systematically documents or ranks these findings today.

## Solution

A full-codebase review producing severity-ranked, evidenced findings published as tickets under
this feature, followed by implementation of the agreed fixes within v1.2 on existing seams,
TDD where behavior changes. The review runs after the other v1.2 features so it also audits the
new localhost-fallback and localization code.

## User Stories

1. As a user, when a CFX/catalog/status endpoint hangs, I want bounded waits, so that the UI
   never sits busy for minutes on the connect path.
2. As a user, I want saved-server and settings writes to be atomic, so that a crash or power
   loss never truncates my data files.
3. As a user, I want untrusted inputs (catalog frames, CFX JSON, saved JSON, CitizenFX.ini,
   icon bytes) parsed defensively, so that malformed data degrades instead of crashing.
4. As a user, I want the per-minute enrichment loop to minimize request volume, so that the
   launcher is a good CFX API citizen and refreshes stay fast.
5. As a user, I want the saved-server refresh to fetch icons concurrently, so that refresh
   completes in bounded time even with many rows.
6. As a user, I want startup work minimized and off the dispatcher, so that the window appears
   fast.
7. As a user, I want process launching audited for argument quoting/injection, so that addresses
   and names can never smuggle shell arguments.
8. As a maintainer, I want every finding recorded as a ticket with severity, evidence, affected
   seam, and proposed fix, so that decisions and fixes are traceable.
9. As a maintainer, I want fixes implemented through the existing seams (injectable time,
   process, HTTP, DNS, storage), so that each behavior change lands with tests and no test
   reaches the real OS/network.
10. As a maintainer, I want the suite green and the portable publish pipeline intact after the
    fixes, so that the deliverable stays reproducible.

## Implementation Decisions

- Review method: a category sweep, each category producing findings tickets in this feature's
  issues directory with severity (Blocker / High / Medium / Low) and an optional performance tag:
  1. Network services: timeout bounds on every outbound call, outage degradation paths, request
     volume/cadence, response size bounds.
  2. Process and OS interop: shell launch argument construction, readiness probes, registry reads,
     mutex/single-instance guard.
  3. Persistence: JSON (settings, saved servers), INI read-modify-write, protobuf frame decoding
     (length bounds, corrupt frames), write atomicity, icon byte handling.
  4. Startup and UI: composition-root cost, dispatcher-blocking operations, collection
     virtualization, memory pressure from large snapshots/icons.
  5. Background loops: cadence, cancellation, re-entrancy (concurrent refresh/save), exception
     containment.
  6. Publish configuration: single-file flags, compression, trimming stance, dependency surface
     (Google.Protobuf/Grpc.Tools and friends).
- Known seed findings from the initial scan (to be confirmed, ranked, and fixed or explicitly
  waived): default HTTP timeout leaving the connect path hang-prone; sequential per-row icon
  fetches in the saved-list refresh; per-minute per-row icon-version checks multiplying requests;
  non-atomic settings/servers file writes; `explorer.exe` argument quoting audit; protobuf frame
  length validation bounds.
- Fixes land on existing seams (injectable HTTP/time/process/DNS/storage); a fix requiring a new
  seam must be justified in its ticket (and get an ADR if architectural).
- Behavior-preserving refactors ride along under the green suite; user-visible changes get tests
  first.
- Findings on the new v1.2 features (localhost fallback, localization) fold into this review's
  tickets.

## Testing Decisions

- Good tests assert external behavior at the touched seam: bounded-wait behavior via injected
  time/HTTP fakes, atomicity via temp-file tests (write, corrupt, kill-simulation where
  feasible), parsing robustness via malformed fixtures (existing raw-string JSON fixtures and
  protobuf frame fixtures), request-volume assertions via the routed fake handler's counters.
- No test touches real network, processes, or the OS install (existing rule; seams only).
- Each fix keeps the suite green; performance fixes assert observable behavior (counts, timing
  through fake providers), not internal details.
- Prior art: fake HTTP handler with request counts, fake time provider, temp-file helpers,
  protobuf frame fixtures — all already in the test project.

## Out of Scope

- TLS certificate pinning, code signing, antivirus/defender whitelisting, obfuscation.
- IL trimming, AOT, or publish-pipeline redesign.
- Formal third-party penetration testing or fuzzing campaigns; the sweep is code-review-driven.
- Denial-of-service hardening beyond sane input bounds.
- New features; only findings-driven fixes and refactors.

## Further Notes

- Order: runs last in v1.2 so the sweep covers the new code.
- The findings list is the deliverable even where the decision is "fix later" — the ticket
  records why.
- Severity ladder meaning: Blocker = data loss/hang reachable by normal use; High = security or
  reliability flaw with a plausible trigger; Medium = robustness/perf debt; Low = hygiene.
