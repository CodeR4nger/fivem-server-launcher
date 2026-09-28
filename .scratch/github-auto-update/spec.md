# Feature: GitHub release auto-update (notify + one-click)

Status: ready-for-agent

## Problem Statement

The launcher is distributed as a portable single-file exe built and published by hand. Users have
no idea when a new version exists and must notice releases themselves, download manually, and
replace the file — most will simply stay on old versions forever.

## Solution

On startup the launcher checks the latest GitHub release of `CodeR4nger/fivem-server-launcher`;
when a newer version exists it shows a small non-blocking banner with the new version. One click
downloads the release asset, stages it, and swaps it in on restart — no elevation, fully
portable. Publishing releases is automated by a GitHub Actions workflow so there is always a
consistent artifact to update from.

## User Stories

1. As a user, when a new release is published, I want a small banner on next launch telling me
   the new version, so I can update at will.
2. As a user, I want a one-click UPDATE button on the banner that downloads and installs the new
   version, so I do not manually manage files.
3. As a user, I want the update applied by restarting the launcher itself, so the swap is
   seamless (download -> relaunch into the new exe).
4. As a user, I want to dismiss the banner and keep using the current version, so updates are
   never forced.
5. As a user, when the update check fails (offline, rate-limited, GitHub down), I want the
   launcher to start normally with no error, so outages never block usage.
6. As a maintainer, I want pushing a version tag to publish a release automatically with the
   portable exe built by CI using the documented build+publish commands, so releases exist
   without manual work.
7. As a user, I want the launcher to never downgrade or install anything untagged, so only real
   releases apply.

## Implementation Decisions

- Version source: a single version constant in the app (AppInfo-family, e.g. 1.2.0) compared
  against the latest release's tag (vX.Y.Z); no assembly-version plumbing beyond that single
  source of truth.
- Check mechanics: anonymous GitHub API `releases/latest` against
  `https://api.github.com/repos/CodeR4nger/fivem-server-launcher`, after startup (never
  blocking), outage -> silent (sanctioned swallow set only). A new seam (release feed) wraps it,
  injected like every other service, so tests use fake HTTP.
- Banner UI: overlay element in the main view bound to VM state (new version available, update
  in progress, failed) — non-modal, dismissible; one UPDATE command.
- Install choreography (portable single exe): download the tagged asset to a temp file, verify
  sanity (asset name/size), then swap-on-restart: rename the running exe (allowed on Windows
  even while running), move the new exe into its place, relaunch the new exe and exit the old
  one. All shell/file steps behind the existing seam pattern (process starter + file seams) so
  the choreography is testable with fakes.
- Release publishing (recommended, needs user confirmation at ticket time): GitHub Actions
  workflow that builds exactly per AGENTS.md's documented build+publish commands on tag push
  and attaches `CFXLauncher.exe` to a release. Alternative (declined work stays out of scope):
  keep manual publishing and only build the in-app updater.
- No auto-update without consent, no channels, no deltas.

## Testing Decisions

- Release-feed tests: fake GitHub JSON fixtures (latest release, no releases, malformed body)
  for version parsing and comparison.
- Update-application tests: choreography via fakes (feed says newer, download succeeds/fails,
  swap steps in order, relaunch invoked); no real network or files in tests.
- Banner VM tests: state transitions (available/dismiss/in-progress/failed), command wiring.
- CI workflow: verified by a real tagged run at implementation time.

## Out of Scope

- Forced or scheduled updates, delta patches, update channels/betas, code signing, update
  telemetry, updating anything other than the portable exe.

## Further Notes

- Runs last per the agreed order (A B C D); the banner inherits the custom control styling.
- Open decision for the implementation session: confirm the GitHub Actions release workflow as
  part of this feature (recommended) or restrict to the in-app updater only.
