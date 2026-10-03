# 05: Forwarding to a running instance (IPC)

**What to build:** Double-clicking a shortcut while the launcher is already running brings
the running window forward and performs the connect in the running instance — the shortcut
always works, no matter the launcher's state.

**Blocked by:** 01 (the duplicate branch reuses the `--connect` parse).

**Status:** ready-for-agent

- [ ] Duplicate instance: the single-instance guard fails → the existing window is
     restored/foregrounded by the existing activator → the address is written to the
     `Local\` named pipe (one UTF-8 line) with a ~2 s timeout → the duplicate exits; any
     failure exits silently exactly as today
- [ ] First instance: an accept loop owned by the composition root, started after guard
     success and cancelled on exit (the mutex-disposal lifecycle)
- [ ] An incoming address is marshaled to the UI dispatcher and handed to a public
     view-model method following the browser-connect precedent: silently ignored when busy
     (no queueing), otherwise fill address + connect; transient UI states (dialog open, dev
     mode, browser overlay) are not micro-managed
- [ ] A forwarded connect persists the last-server memory on success like any connect
- [ ] The listener and forwarder are real classes with no interfaces (no surrounding logic
     class exists), tested directly against a real in-process pipe pair: roundtrip, timeout,
     malformed line, cancellation
- [ ] The guard still runs before everything — the duplicate can never reach the view model
     directly; the pipe is the only channel
