# 01: Classify and validate the 4 address forms in ServerAddress

**What to build:** `ServerAddress` (single source of the address form) learns to recognize and validate the four forms DOC.md requires: bare `CFX ID`, `cfx.re/join/<id>` URL (with or without scheme), `IP:port` and `domain:port`. Each form validates its syntax (octets 0-255, port 1-65535, alphanumeric domain labels with `-`, no scheme/path/user@, no whitespace) and exposes extraction of the relevant data for each form (`cfxId`, `ip:port`, `host:port`). The existing `ExtractCfxId` and `HasServerFormWithNonEmptyId` APIs are re-expressed on the new classifier without breaking their call-sites.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] A classifier (enum) returns an address's form: `CfxId`, `CfxJoinUrl`, `IpPort`, `DomainPort` or unknown/invalid.
- [x] `cfx.re/join/<id>` and `https://cfx.re/join/<id>` are classified as `CfxJoinUrl` and extract the id (current behavior preserved).
- [x] A bare id without `.`, without `:` and without `cfx.re/join/` is classified as `CfxId`.
- [x] A valid `IP:port` (4 octets 0-255 + port 1-65535) is classified as `IpPort`; out-of-range octets or port 0/65536 → invalid.
- [x] A valid `domain:port` (alphanumeric labels with `-` joined by `.`, port 1-65535, no scheme/path/user@) is classified as `DomainPort`; violations → invalid.
- [x] Any whitespace in any form → invalid.
- [x] `ExtractCfxId` and `HasServerFormWithNonEmptyId` keep compiling and passing their existing tests after being re-expressed on the classifier.
- [x] Pure tests in `ServerAddressTests` (no HTTP, no filesystem), repo Given/When/Then style.

## Comments

- TDD RED→GREEN→REFACTOR completed with 13 new tests in `tests/.../Domain/ServerAddressTests.cs`. Full suite 75 green (62 previous + 13 new).
- Decision along the way (TLD rule): a `domain`'s TLD must contain at least one letter so `999.56.120.52:30320` (IP with invalid octet) is not confused with a numeric domain.
- REFACTOR applied: `HasServerFormWithNonEmptyId` and `Classify` share `IsValidCfxId` (DRY), with a green suite.
