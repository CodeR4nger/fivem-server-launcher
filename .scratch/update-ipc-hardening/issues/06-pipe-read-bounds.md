# 06: Read timeout and line cap on the connect pipe

**What to build:** A client that connects and never writes, or writes more than the line cap, is dropped without dispatch and later clients are still served. Spec decision 6.

**Blocked by:** 05

**Status:** open
