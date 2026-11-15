# unexpected-failure-status

Status: done

See ../spec.md section 4.

Notes: connect is pinned directly (a fault outside the outage set escaping the CFX lookup,
both through `ConnectAsync` and the startup connect) and the command routing through
`ConnectBrowserServerCommand` (a wrong parameter type). Open client and dev launch share the
same `RunBusyFlowAsync` helper; no current seam can make them throw (`GameLauncher` catches
every start failure into `StartFailed`), so they are covered by the shared code path, not by a
dedicated test. An `IOException`/`UnauthorizedAccessException` inside the startup connect now
ends as "Something went wrong" instead of reaching the `InitializeAsync` guard's "startup
failed" line (the connect owns its failures; the guard still covers the install probe).
