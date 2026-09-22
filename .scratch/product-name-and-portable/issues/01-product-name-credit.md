# 01: Product name + credit visible

**What to build:** the launcher presents itself as **CFX Launcher** with a **by CodeRanger**
credit in the window title area and a footer. A single `AppInfo` source of truth (product name,
author, combined title) is pinned by a unit test, and the main window binds its `Title`, the
custom title-bar text, and a new footer `TextBlock` to it.

**Blocked by:** None (can start immediately).

**Status:** resolved (suite 425 green; title bar + footer bound to `AppInfo`)

- [x] `AppInfo` exposes `ProductName` ("CFX Launcher"), `Author` ("CodeRanger") and a combined `Title`; tests pin the exact strings.
- [x] `MainWindow.xaml` `Title` and the title-bar text bind to `AppInfo` (no hardcoded "FiveM Server Launcher" remains).
- [x] A footer showing the credit appears in the main window, bound to `AppInfo`.
- [x] Build clean; suite 425 green (+3 AppInfo tests).