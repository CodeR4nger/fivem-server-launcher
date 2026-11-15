# 02: Accept only https github.com download URLs

**What to build:** The feed answers no update unless `browser_download_url` is an absolute https URL on host `github.com` (default port, no userinfo). Spec decision 2.

**Blocked by:** none

**Status:** resolved

## Answer

`GitHubReleaseFeed.IsTrustedDownloadUrl`: absolute, scheme exactly https, default port, no userinfo, host exactly `github.com` (case-insensitive); anything else is a null update. Look-alike hosts, userinfo tricks, http, file and relative URLs are pinned by a theory.
