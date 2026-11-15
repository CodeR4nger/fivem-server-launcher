# 04 — Icon host allowlist

Status: open

## What

`IsSafeIconUrl` accepts only `cfx-server-metadata.cfx-services.net` (case-insensitive), on
top of the existing absolute/https/no-userinfo rules. Justified by the measurement in the
spec (25,171 of 25,171 published icons).

## Tests

- Other https hosts, look-alike hosts (suffix/prefix tricks, subdomains) → null, zero
  requests.
- The allowed host in a different letter case is fetched.
