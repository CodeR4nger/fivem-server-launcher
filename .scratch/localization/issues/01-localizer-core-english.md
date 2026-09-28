# 01: Localizer core with English source-of-truth dictionary

**What to build:** The localization foundation: a localizer service seam that resolves stable
keys to strings for the current language from embedded JSON dictionaries, with English as the
key-set source of truth and guaranteed fallback (missing key -> English; missing/corrupt
language file -> English), an injectable system-culture provider for the follow-system default,
a language-changed event for live switching, and the language list with native display names.
The English dictionary covers every current UI string (labels, statuses, tags, tooltips), so
the app still renders exactly today's English UI through the new seam.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Key resolution returns the current language's string for a key
- [x] Missing key in a language falls back to English; missing/corrupt language file degrades to
      English without throwing
- [x] Follow-system default resolves via an injected culture provider (exact culture, then
      neutral parent, else English)
- [x] Changing the active language raises the change event
- [x] Available languages enumerate with native display names
- [x] Completeness audit: every English key exists in every shipped language file
