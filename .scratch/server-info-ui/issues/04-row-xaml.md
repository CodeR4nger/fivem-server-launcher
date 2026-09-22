# 04: Row XAML (icon + game tag + status) + wiring

**What to build:** saved-server row template renders `<ICON> <name> [gameTag] <status>` when
`CfxId`/enrichment is present: the per-game colored tag (FiveM orange / FiveM Enhanced blue /
RedM red, via `GameClientToBrushConverter`) shows once the game is known; the status slot shows
`players/max` when online, `OFFLINE` for a CfxId row not in the current catalog, `UNRESOLVED`
when no CfxId; plain name otherwise; edit pencil behavior preserved. Composition root wires the
enrichment service + timer into the VM.

**Blocked by:** 03.

**Status:** ready-for-agent

- [x] Enriched row layout (icon + name + `players/max`); plain fallback for null.
- [x] Offline absence signal without fabricated players.
- [x] Composition root wiring done; manual visual verification PENDING (awaiting user to run app).
- [x] Per-game colored tag right of the name (`GameClientToBrushConverter`, shows via `HasGame`).
- [x] Status slot shows `OFFLINE`/`UNRESOLVED` text instead of a fabricated count.

## Acceptance (manual E2E)

With the real network: saved CFX-form server shows icon + game tag + `n/max`; saved IP:port server whose
server is published gets enriched shortly after save; count updates after ~1 minute; a CfxId row
not in the catalog shows `OFFLINE`; an unroutable/unpublished row shows `UNRESOLVED`.