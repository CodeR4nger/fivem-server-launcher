# 06: Read timeout and line cap on the connect pipe

**What to build:** A client that connects and never writes, or writes more than the line cap, is dropped without dispatch and later clients are still served. Spec decision 6.

**Blocked by:** 05

**Status:** resolved

## Answer

`ConnectRequestListener` gained optional `readTimeout` (default 2 s) and `maxLineBytes` (default 2048) ctor params and reads the line itself (`ReadBoundedLineAsync`), keeping ReadLine semantics (`\r\n`/`\n`, final unterminated line dispatches, empty EOF dispatches nothing). A timeout or over-cap read throws inside the loop and the connection is dropped without dispatch; only cancellation of the listener's own token ends the loop. Defaults are used in `App.xaml.cs`, which was not touched.
