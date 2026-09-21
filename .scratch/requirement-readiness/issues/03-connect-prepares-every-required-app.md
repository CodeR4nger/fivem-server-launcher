Status: resolved
Type: task
Blocked by: 02

# 03: connect flow prepares every required app (no missing gate)

`ViewModels/MainViewModel`: remove `FindMissingRequirementsAsync` and the `IRequirementReadiness`
dependency from the constructor. Loop every required app (`SteamRequired == true` /
`DiscordRequired == true`) through `_preparer.TryPrepareAsync` — the preparer self-returns `true`
when already ready, so the UX stays "nothing extra when all are ready". Copy unchanged:
"Starting {app}..." while preparing; "Could not start {app}" + no launch on failure.

Composition root `App.xaml.cs`: stop passing `readiness` into `MainViewModel` (the preparer keeps
it).

RED->GREEN at the VM seam, adapting both readiness fakes:
- required + already ready -> launch, nothing started, one ready check
- required + not ready -> "Starting Steam..." -> ready -> launch (starter called)
- required + **running but not ready** -> waited on without restart -> launched (the reported flow)
- never ready -> "Could not start Steam", no launch, not busy
- start throws -> "Could not start Steam", no launch

Done when `dotnet test` green and the two docs bullets (AGENTS.md + DOC.md) are updated.