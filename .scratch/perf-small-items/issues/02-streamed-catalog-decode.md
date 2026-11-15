# streamed-catalog-decode

Status: done

See ../spec.md section 2.

## Measurement (2026-11-15)

Real live streamRedir payload captured the same day: 20,700,892 B, 33,045 frames, largest
frame 12,081 B. `GC.GetTotalAllocatedBytes(precise: true)` around one
`ServerCatalog.GetSnapshotAsync()` served by `StreamingHttpMessageHandler` (no
Content-Length, read as it arrives), three runs each (a throwaway probe test, not committed):

| path | allocated per download+decode |
| --- | --- |
| before (MemoryStream + ToArray + Decode(byte[])) | 228.5 MB (228,763,064 / 228,505,832 / 228,507,768) |
| after (DecodeAsync from the stream) | 157.6 MB (157,639,680 / 157,594,608 / 157,594,576) |

The parsed object graph alone is ~140 MB (decode of an in-memory byte[]), so payload
buffering dropped from ~88 MB to ~17 MB of allocation. Peak live payload: before, the
MemoryStream buffer (capacity doubled to 32 MB) and the 20.7 MB `ToArray` copy coexisted
(~54 MB), then 20.7 MB stayed alive through the whole decode; after, one 16 KB frame buffer
(it would only grow for a frame over 16 KB — none exists in the live catalog).
