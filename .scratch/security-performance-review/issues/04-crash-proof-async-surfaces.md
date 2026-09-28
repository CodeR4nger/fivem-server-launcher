# 04: Crash-proof async surfaces

**What to build:** Commands and startup run through async-void (`AsyncRelayCommand.Execute`,
dispatcher-queued `InitializeAsync`), and several paths throw exceptions that nothing catches: a
malformed `/single/` body (`JsonException` escapes `CfxService`), disk faults in the fire-and-forget
id capture (surfaced at save via `Task.WhenAll`), and startup IO. One such exception takes down the
process. Degrade every async surface to a visible status or silent continuation instead of a crash,
without widening the sanctioned swallow sets for outage classes.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] A malformed CFX JSON body degrades to "cannot resolve" (same as outage), never escapes
- [ ] The id capture degrades on disk/serialization faults without crashing the save flow
- [ ] Startup initialization survives non-outage faults (installation probing, settings IO)
- [ ] Command execution failures leave IsBusy cleared and a status message shown
