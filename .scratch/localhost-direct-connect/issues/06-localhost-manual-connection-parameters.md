# 06: Localhost manual connection parameters (sub Dev Mode for loopback servers)

**What to build:** A saved loopback server (localhost / 127.0.0.1, any port form) can carry manual
connection parameters, entered in an extra dialog section that appears only for loopback
addresses: an optional CFX ID, game build, pure mode, and game client. At connect time the
launcher resolves requirements from that specific id (never from "the first server found in the
list"); when the id resolves, its published build/pure/game win over the manual values; when
there is no id or the id is delisted/unreachable, the manual values apply. The connect address
always stays the direct loopback address — never the join form — so the local server connects
even while delisted. Manual Steam/Discord flags keep their additive rule.

**Blocked by:** 05 (hidden loopback endpoints ignored by matching).

**Status:** done

- [x] A loopback-address predicate recognizes localhost/127.0.0.1 forms (any port form,
      case-insensitive) and rejects everything else
- [x] Saved servers carry optional game build, pure mode, and game client overrides; persisted
      and round-tripped; old files deserialize with nulls
- [x] Add/edit dialog shows the CFX ID / GAME BUILD / PURE MODE / GAME section only when the
      typed address is loopback; values prefill on edit and persist on save
- [x] Connect with a resolvable manual id: requirements and game client come from that exact
      server; manual build/pure/game are ignored (id wins); connect launches direct to the
      loopback address
- [x] Connect with a delisted/unreachable id or no id: manual build/pure/game apply; connect
      still launches direct
- [x] Connect with no overrides at all: today's unvalidated direct connect, unchanged
- [x] Manual Steam/Discord flags still merge additively into the effective requirements
- [x] Enrichment (name/players/icon) keys off the manual cfx id like any saved row
