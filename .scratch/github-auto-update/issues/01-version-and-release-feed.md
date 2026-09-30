# 01: Version constant + release feed seam (is there a newer release?)

**What to build:** The launcher knows its own version through a single constant (AppInfo family)
and can answer "is there a newer release?" — nothing else changes yet. A new release-feed seam
wraps the anonymous GitHub `releases/latest` API for the launcher's repository, injected like
every other service, and reports the latest published release only when it carries a strictly
newer, well-formed `vX.Y.Z` tag — answered with that release's version and its portable
`CFXLauncher.exe` asset (name, size, download URL). Everything else is a silent "no update":
same version, an older tag (never downgrade), a garbage or missing tag (never install anything
untagged), a missing or misnamed asset, 404 (no releases), a malformed body, or a network
outage — the sanctioned swallow set only, never an exception to the caller, never anything
that could block startup.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Answer

Delivered as three production pieces: the `AppInfo.Version` constant (`"1.3.0"`, the
unreleased v1.3 the feature ships under — the single version source, pinned parseable by a
test so a malformed constant fails the suite, not the user), the pure `AppVersion.TryParseTag`
(strict three-part numeric after an optional `v`; bare `X.Y.Z` accepted per the user decision
at seam agreement, everything else — two/four parts, letters, prerelease suffixes — `null`;
note `System.Version.TryParse` trims surrounding whitespace, so a trailing-space tag would
parse — irrelevant in practice because GitHub tags cannot contain spaces), and the release-feed seam (`IReleaseFeed`/`GitHubReleaseFeed` over
`HttpClient`, `LauncherUpdate` record) following the status-service file convention. The feed
compares the parsed tag against the parsed constant with `System.Version` ordering (no custom
comparison code) and selects the asset by exact `CFXLauncher.exe` name among any decoys.
Swallow set: `HttpRequestException`/`TaskCanceledException`/`JsonException` → `null`, mirroring
`CfxStatusService`; 404 reaches the same exit through its error body (no `tag_name`). The
request carries a `User-Agent` per request (the GitHub API rejects anonymous requests without
one; the shared composition-root client stays untouched).

Two implementation notes worth remembering: GitHub's API is snake_case, so
`PropertyNameCaseInsensitive` alone cannot bind `tag_name`/`browser_download_url` — the DTOs
carry explicit `JsonPropertyName` attributes (the status-service pattern worked only because
statuspage is camelCase). And creating the `Tests.Core` namespace hijacked every unqualified
`Core.Enums.` reference in three Domain test files (they had resolved through the root
namespace because `Tests.Core` did not exist); those files now import the Enums namespace and
use bare `GameClient`, matching the other 30+ test files.

Review follow-ups applied: `LauncherUpdate.Version` renamed to `Tag` (it carries the release's
tag verbatim, not a parsed version), the feed constructs it with the `AssetName` constant
instead of re-reading `asset.Name!`, the timeout half of the swallow set (`TaskCanceledException`,
via a self-contained throwing handler) and the 403 rate-limit body (spec story 5) are now
pinned by fixtures, and the spec's two surviving CI-workflow statements were removed so the
declined decision is recorded consistently.

- [x] A single version constant is the app's only version source (no assembly-version plumbing
      beyond it; it carries the version the build represents)
- [x] The latest published release with a strictly newer well-formed tag is reported with its
      version and its `CFXLauncher.exe` asset (name, size, download URL)
- [x] Same version, older tag, malformed or absent tag → no update (never downgrade, never
      untagged)
- [x] A release whose portable-exe asset is missing or misnamed → no update
- [x] 404 (no releases), malformed body, outage/timeout → no update, never throws (sanctioned
      swallow set only, mirroring the status-service pattern)
- [x] Version tag parsing/comparison is a pure unit with fixture-driven tests: newer/same/older,
      valid tag forms, garbage tags
- [x] Feed tests use fake HTTP fixtures (latest release, no releases, malformed body); no real
      network
- [x] Suite green (RED → GREEN → REFACTOR) — 788 tests green

**Spec:** `.scratch/github-auto-update/spec.md`
