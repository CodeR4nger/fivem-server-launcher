Status: resolved
Type: spec

# Post-review fixes for the ip-domain-resolution phase

## Problem Statement

The code review of the diff `5dc7b7f...HEAD` (Standards and Spec axes, `code-review` skill) found code, spec, and documentation findings after the `ip-domain-resolution` phase. There are no critical functional bugs (all checkboxes of the 5 original tickets are met), but there are: (a) a spec↔code discrepancy in domain validation (spec.md:44 "1+ labels" vs implementation `labels >= 2` + TLD-with-letter), (b) `InvalidAddressException` receives the extracted `cfxId` instead of the original address (spec.md:30 does not fix the contract), (c) duplication in the host:port split and in `gamename` access, (d) dead code (`IsCfxJoinUrl`), (e) `catch (Exception)` that masks programming errors, (f) documentation contradiction: AGENTS.md says `EndPoint` is a "connection endpoint, not the id" but `ServerResolver.BuildValidatedProfile` uses `EndPoint` from the catalog frame as `CfxId`.

This spec first settles the scope decisions (domain validation, exception contract, `EndPoint` semantics in the catalog) and then the code tickets that implement them.

## Solution

- **Domain validation (decided):** **1+ labels** are accepted (aligns with spec.md:44). Single-label cases (`localhost:30120`) are classified as `DomainPort` if the label is valid. The "TLD with a letter" rule applies only when there are 2+ labels (prevents `999.56.120.52:30320`, an IP with an invalid octet, from passing as a numeric domain). This rule is **written into spec.md** (today it lives only in a ticket comment).
- **Exception contract (decided):** `InvalidAddressException` is always constructed with the **original address** the user entered (not the extracted id). All resolver call sites pass the input address untransformed.
- **`EndPoint` semantics in the catalog (decided):** in `streamRedir` frames, `EndPoint` is the **canonical server id** (cfx id); AGENTS.md describes the `EndPoint` of the **`/single/` response** (which IS a connection endpoint and is NOT used as an id). AGENTS.md documentation is reconciled to distinguish both, without changing `CfxId = server.EndPoint` in the resolver.
- **Other code findings** become the refactor/fix tickets listed below, all with TDD (RED→GREEN→REFACTOR) and without changing external behavior except where a decision requires it.

## User Stories

1. As a player, I want to be able to join `localhost:30120` when I run a local server, so I don't depend on a 2+ label domain (validation relaxation).
2. As a player, I want an invalid-address error to show or preserve the address exactly as I typed it, so I can diagnose the failure quickly.
3. As a developer, I want the resolver/catalog code not to duplicate the `host:port` split or `gamename` access, to keep it DRY.
4. As a developer, I want unexpected network errors (non-CFX) not to be silenced, so bugs are not hidden.

## Implementation Decisions

- `ServerAddress.IsValidDomain` is relaxed to 1+ labels: `labels.Length >= 1`, each label valid, and if `labels.Length >= 2` the TLD must contain a letter. New tests in `ServerAddressTests` for `localhost:30120` (DomainPort) and for an invalid 4-octet IP like `999.56.120.52:30320` (still Unknown → exception).
- `ServerResolver`: `InvalidAddressException` with the original address in the 3 paths that currently throw it (whitespace, unknown form, CFX null). Test verifies the message/exception uses the original input.
- **DOCUMENTATION:** `AGENTS.md` "Current state" distinguishes: `EndPoint` in the `/single/` response = connection endpoint (not used as id); `EndPoint` in catalog frames = canonical cfx id. The resolver line and the `CfxService` line are updated as appropriate.
- DRY refactor 1 (ticket 02): shared `SplitHostPort` method in `ServerAddress` or a helper, used by `IsIpPort`/`IsDomainPort` and by `ServerResolver` (replaces `GetHost`/`GetPort`).
- DRY refactor 2 (ticket 03): `CfxVars.TryGetGameClient(IDictionary<string,string>? vars)` — returns `(bool found, GameClient? gameClient)` or an equivalent pattern — used by `CfxService` and `ServerResolver`, centralizing the `"gamename"` key and the null-guard.
- Dead code (ticket 04): remove `ServerAddress.IsCfxJoinUrl` (no callers). `LookupByEndPointAsync` is **kept** (documented in AGENTS.md as part of the catalog seam; used by tests); it is made clear why it is exposed (part of the catalog contract, ready for future UI/id lookup).
- Narrow catch (ticket 05): `ServerCatalog.GetServersAsync` catches only `HttpRequestException` and `TaskCanceledException` (outage/network); any other exception propagates. The public API does not change (still returns no-match on outage).
- No UI changes, no changes to existing public contracts (signatures of `ServerAddress`, `ServerProfile`, `ServerCatalog` remain except for internal refactors).
- Tickets 02-05 do not change observable behavior except 05 (propagating programming errors); 01 does change classification (localhost) and the exception contract (original address). Corresponding tests in each ticket.

## Testing Decisions

- TDD per slice: each ticket in `RED → GREEN → REFACTOR` with the full suite green at the end.
- Tests for ticket 01 in `ServerAddressTests` (localhost classification + invalid IP persists as Unknown) and `ServerResolverTests` (exception with original address).
- Tests for ticket 02: extraction refactor, the existing suite is the safety net (no new tests unless the refactor warrants it).
- Ticket 03: `CfxServiceTests` and resolver tests stay green after consolidation; optionally a direct test of `CfxVars.TryGetGameClient`.
- Ticket 04: remove no test; the full suite covers non-regression.
- Ticket 05: `ServerCatalogTests` with `FakeHttpMessageHandler(throwOnSend: true)` (HttpRequestException → no match) and a new case: a handler that throws `InvalidOperationException` → the exception propagates (not swallowed).

## Out of Scope

- Any new functionality (UI, unvalidated-server warning, catalog favicons/players).
- Reopening the `ip-domain-resolution` phase (already-resolved specs and tickets stay as they are).
- Changing `ServerProfile.Address`/`IsCfxValidated` or the catalog contract.
- Noisy refactor of `ServerAddress` beyond the host:port split and the dead code.

## Further Notes

- Review doc (findings): `.scratch/...` no; the review lives in the conversation. Code references: `src/FiveMServerLauncher/Domain/ServerAddress.cs` (`IsCfxJoinUrl`, `IsIpPort`, `IsDomainPort`), `Domain/ServerResolver.cs` (`GetHost`, `GetPort`, `BuildValidatedProfile`), `Service/CfxVars.cs`, `Service/CfxService.cs`, `Service/ServerCatalog.cs` (`GetServersAsync` catch), `src/FiveMServerLauncher/AGENTS.md`.
- Original phase spec: `.scratch/ip-domain-resolution/spec.md`; tickets 01-05 resolved.
- After closing: update AGENTS.md (current Statement: EndPoint note, if applicable) and commit with English conventional commits.
