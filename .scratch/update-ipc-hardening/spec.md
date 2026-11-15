# Update and IPC hardening

Session 2 of `.scratch/review-hardening/plan.md` (wave 1). Hardens the GitHub auto-update and
the single-instance connect pipe. Scope: `Launch/UpdateApplier`, `Service/GitHubReleaseFeed`,
`Service/LauncherUpdate`, `Service/ConnectRequestListener`, `Service/ConnectRequestForwarder`
and their tests. `MainViewModel` and `App.xaml.cs` are not touched (all new behaviour rides
defaults inside the classes).

## Live facts (checked 2026-11-15, anonymous GitHub API)

- `releases/latest` for this repo answers `v1.3.1`; its `CFXLauncher.exe` asset publishes
  `"digest": "sha256:11f72d01…a11cf"` (prefix `sha256:` + 64 lowercase hex) and
  `"size": 65601097`. All five releases ever published (back to `1.0.0`, 2026-09-22) carry a
  digest in the same format — none lacks it.
- `browser_download_url` is `https://github.com/CodeR4nger/fivem-server-launcher/releases/download/<tag>/CFXLauncher.exe`
  and answers `302` to `https://release-assets.githubusercontent.com/github-production-release-asset/…`.
- The whole `releases/latest` body is about 5 KB.

## Decisions

### 1. Digest (integrity)

`LauncherUpdate` gains an optional trailing `Sha256` (lowercase hex, null when unpublished;
optional so pre-feature constructions keep compiling).

- Feed: digest absent/null → `Sha256 = null`. Digest present and well-formed
  (`sha256:` + 64 hex, case-insensitive) → carried, normalized to lowercase. Digest present
  but unusable (other algorithm, wrong length, non-hex) → **no update**: a published digest we
  cannot verify is never silently downgraded to the size check.
- Applier: after the size check, when `Sha256` is non-null the staged file's SHA-256 must
  equal it, else rollback and `false`. When `Sha256` is null the size check alone decides.

Trade-off of the absent-digest fallback: the digest and the download URL arrive in the same
API response over the same TLS connection, so anyone able to strip the digest can equally
replace it with one matching their payload — requiring presence buys no protection against
that attacker. What the digest does protect is the second hop (the asset download, its
redirect and CDN) and plain corruption. Refusing on absence would only turn a GitHub API
change (field removed/renamed) into a silent end of auto-update. So: verify whenever
published, fall back to size when absent. Today the fallback is dead code in practice —
every live release publishes a digest.

### 2. Download URL

The feed accepts only an absolute `https` URL whose host is exactly `github.com`, default
port, no userinfo; anything else is no update. The redirect target
(`release-assets.githubusercontent.com`) is deliberately not pinned: it is GitHub's CDN
choice and has changed before (`objects.githubusercontent.com`), .NET never follows an
https→http redirect, and the digest check is what vouches for the bytes whatever host
served them.

### 3. Stale staged files

The best-effort sweep covers `<exe>.new-*` as well as `<exe>.old-*` (same rule: a locked
file never blocks). Safe because the launcher is single-instance — no other live applier can
own a staged file.

### 4. Feed body bound

The feed reads at most 1 MB of body (real body ≈ 5 KB, release notes included); a larger
body is abandoned and yields no update. Headers-first read so the cap is real.

### 5. Pipe identity

Named pipes live in one machine-global namespace (the `Local\` prefix means nothing for
pipes), while the mutex is per session. The default name becomes
`{ProductName}.connect.{userSid}.{sessionId}` — derived by one function both ends use — and
both ends pass `PipeOptions.CurrentUserOnly` (server ACL = current user only; client
verifies the server's owner). Accepted edge: an elevated launcher and a non-elevated
duplicate (or vice versa) have different owner SIDs, so that forward fails and the duplicate
exits silently — the existing documented degrade.

### 6. Pipe read bounds

The listener reads the line itself: at most `maxLineBytes` (default 2048 — the longest
valid address is far shorter) within `readTimeout` (default 2 s, the forwarder writes
immediately after connecting). A timed-out or oversized connection is dropped without
dispatch and the loop accepts the next client. Unchanged: empty lines dispatch verbatim, a
final line without newline dispatches, EOF without data dispatches nothing, handler
exceptions never kill the loop.

## Verification

Tests only: real temp-dir I/O for the applier, real pipes (unique name per test class) for
the IPC. The real update path cannot be exercised end to end without publishing a release.
