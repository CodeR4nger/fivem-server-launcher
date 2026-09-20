# 01: Harden and seal the factory validation

**What to build:** the `Create` factory of `FiveMLaunchOptions` rejects any degenerate address, not just empty or prefix-less ones: an address with the `cfx.re/join/` prefix but no id, or with internal/trailing spaces, does not build options. Also, the validity invariant stops being bypassable: the fields cannot be populated by skirting the factory (private positional constructor or equivalent), so that no invalid state is serialized silently.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Given `cfx.re/join/` (prefix without id), `Create` throws `InvalidAddressException`
- [x] Given `cfx.re/join/y4lg95 ` (trailing whitespace), `Create` throws `InvalidAddressException`
- [x] Given `cfx.re/join/y4 lg95` (internal space), `Create` throws `InvalidAddressException`
- [x] Valid addresses (`cfx.re/join/y4lg95`) still build without error (no regression)
- [x] It is not possible to construct `FiveMLaunchOptions` with invalid state outside the factory (blocked at design/compile time)
- [x] The full suite stays green after the change
