# 06: Remove the `DefaultLocalizer` service locator from production code

**What to build:** `Localization/DefaultLocalizer` is a static `Lazy<ILocalizer>` that pins the
culture to a hardcoded `"en-US"` and is used as the default argument of the optional `ILocalizer?`
parameter on `CfxStatusItem`, `SavedServerItem` and `ServerBrowserViewModel`. Delete it and make
`ILocalizer` a required constructor dependency on all three, per the documented composition-root
ownership in `AGENTS.md`.

**Why:** the static instance is only ever reached from tests today — `App.xaml.cs` injects the
shared localizer everywhere — so it is a production-code affordance with no production caller. The
failure mode it invites is nasty: a `DefaultLocalizer` instance is a *second* localizer that never
receives `SetLanguage` calls, so any view model built without an explicit localizer would silently
lock to English and never re-emit on a live language switch. The optional-parameter shape is what
allows the bug; the static is what makes it silent.

**Blocked by:** None.

**Status:** done

**Severity:** medium (latent, not live)

- [x] `DefaultLocalizer.cs` deleted
- [x] `ILocalizer` required (non-optional, non-defaulted) on `CfxStatusItem`, `SavedServerItem`,
      `ServerBrowserViewModel`
- [x] Every test call site that omitted the argument passes one explicitly (~20 sites across
      `MainViewModelTests`, `SavedServerItemTests`, `CfxStatusItemTests`, `ServerBrowserViewModelTests`)
- [x] `dotnet test` green
