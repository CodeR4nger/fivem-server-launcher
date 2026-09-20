# 01: Align domain classification + exception contract with the spec

**What to build:** two fixes decided in the spec amendment:
1. `ServerAddress.IsValidDomain` accepts **1+ labels** (prop: `localhost:30120` → `DomainPort`). The "TLD with a letter" rule applies only with 2+ labels, preventing a 4-octet IP with an invalid octet (`999.56.120.52:30320`) from being classified as a domain.
2. `ServerResolver` always constructs `InvalidAddressException` with the **original address** the user entered (today it passes the extracted `cfxId` when CFX returns null).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `localhost:30120` (and any valid 1-label) → `ServerAddressKind.DomainPort`.
- [x] `999.56.120.52:30320` (4-octet IP with invalid octet) still → `Unknown` (no regression of the TLD fix from the previous phase).
- [x] "TLD with a letter" rule documented in the current phase's `spec.md` (already in `Implementation Decisions`; verify it remains as a written decision, not only in a ticket comment).
- [x] `InvalidAddressException` in the resolver uses the original address in the 3 paths: whitespace/empty, `Unknown` form, and CFX null.
- [x] New tests in `ServerAddressTests` (localhost → DomainPort; invalid IP → Unknown) and `ServerResolverTests` (exception with original address).
- [x] Full suite green.

## Comments
