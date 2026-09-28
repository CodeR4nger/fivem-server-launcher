# 02: Harden process-launch inputs

**What to build:** A crafted saved-server entry (imported/shared `saved-servers.json`) can smuggle
shell arguments into `explorer.exe`: the cfx-id validation accepts quotes/ampersands, the connect
URI concatenates the address unescaped (query/fragment injection), and the executable path is
quoted without escaping. Tighten the cfx-id charset, escape the address into the connect URI, and
escape the executable path when launching via the shell route.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Cfx-id validation rejects characters outside a strict connectable charset (letters, digits,
      dash, underscore)
- [ ] The connect URI escapes the address so `?`, `#`, quotes and friends cannot alter it
- [ ] The executable path cannot break out of its quoting in the shell launch
- [ ] All existing address forms still classify, resolve, and launch exactly as before
