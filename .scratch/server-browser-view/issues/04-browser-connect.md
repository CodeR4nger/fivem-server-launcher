# 04: Connect from the browser

**What to build:** a Connect action on a browser row that runs the identical connect pipeline
as typing a cfx id in the main view (resolution, effective requirements, Steam/Discord
preparation, launch), then closes the browser view so the user sees the usual status messages
and `LastServerAddress` persistence on the main view. Wired via a callback seam from
`ServerBrowserViewModel` into `MainViewModel`/the existing pipeline; no duplicated connect
logic.

**Blocked by:** 02 (ServerBrowserViewModel with filters).

**Status:** resolved

## Answer

`MainViewModel` gained `Browser` (composition-root provided), `IsServerBrowserOpen`,
`OpenServerBrowserCommand` (closes settings/dev/dialog overlays, then loads),
`CloseServerBrowserCommand`, `ConnectBrowserServerCommand` → `ConnectFromBrowserAsync(item)`
(set address = join form, close browser, run the normal `ConnectAsync` pipeline — persistence
included), and `SaveBrowserServerCommand` → prefilled add dialog. App.xaml.cs shares the
single catalog/enrichment/repository into the browser VM. Suite 507 green.

- [ ] Connect delegates with the server's cfx id (join form)
- [ ] Requirement preparation behaves exactly like the main-view connect
- [ ] Browser view closes; status shows on the main view
- [ ] Successful connect persists `LastServerAddress` as usual
- [ ] Suite green (RED → GREEN → REFACTOR)

**Spec:** `.scratch/server-browser-view/spec.md`
