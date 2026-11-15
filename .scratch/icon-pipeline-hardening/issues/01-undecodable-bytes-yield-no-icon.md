# 01 — Undecodable bytes yield no icon, never an exception

Status: open

## What

- `Core/BoundedIconDecode.TryDecode(byte[]) → BitmapImage?`: the bounded frozen decode the
  converter already does, returning null on any failure.
- `IconImageNormalizer.Normalize`: non-WebP bytes are returned untouched only when
  `TryDecode` succeeds; otherwise null.
- `BytesToImageSourceConverter`: uses `TryDecode`; a failure converts to null and is cached
  so the same `byte[]` is not decoded again.

## Tests

- Normalizer: HTML, random garbage and a truncated/corrupt PNG → null; a real PNG → same
  instance.
- Converter: the same three inputs → null, no throw; second convert of the same failing
  instance does not decode again.
- Real binding: `Image.SetBinding(Image.SourceProperty, …)` with the converter over HTML
  bytes on an STA thread does not throw and leaves `Source` null.
- Service: `GetIconAsync` over a URL serving HTML → null.
- Existing enrichment tests that used fake non-image payloads switch to real PNG bytes;
  the pass-through-any-bytes claim test is deleted and rewritten under a new name.
