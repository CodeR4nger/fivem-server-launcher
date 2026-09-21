# 04: Dev Mode UI swap

**What to build:** the main window gains a DEV MODE toggle button on the FiveM panel that swaps the
normal OPEN area for the dev view: Game Build textbox, Pure Mode selector (0/1/2), a primary
LAUNCH button (no Second Client) and a secondary with `-cl2`. Hidden/visible via converters only;
no code-behind.

**Blocked by:** 03.

**Status:** resolved

- [ ] DEV MODE button toggles the panel sections and offers a way back.
- [ ] Build/pure controls bind two-way to the VM and persist on change.
- [ ] LAUNCH and LAUNCH WITH -CL2 invoke the dev launch command with the right parameters.
- [ ] Settings (gear) panel still works; dev UI is distinct from it.
