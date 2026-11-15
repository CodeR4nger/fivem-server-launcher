using System.IO;
using System.Net;

namespace FiveMServerLauncher.Tests.Service;

// Answers every request with a fresh TestHttpContent built by the factory and
// keeps every response it handed out, so tests can see how much of a body was
// pulled and whether the response was disposed.
internal sealed class StreamingHttpMessageHandler(
    Func<TestHttpContent> contentFactory,
    HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    private readonly object _gate = new();
    private readonly List<TestHttpContent> _contents = new();

    public IReadOnlyList<TestHttpContent> Contents
    {
        get
        {
            lock (_gate)
            {
                return _contents.ToList();
            }
        }
    }

    public int RequestCount => Contents.Count;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var content = contentFactory();

        lock (_gate)
        {
            _contents.Add(content);
        }

        return Task.FromResult(new HttpResponseMessage(status) { Content = content });
    }
}

// A body served as a stream, without declaring Content-Length (the cap must be
// enforced while reading, not by trusting a header). Behaviours:
// - Payload: the bytes served (repeated until TotalLength when it is set).
// - FailAfter: throws IOException once that many bytes were served.
// - StallAfter: never completes after that many bytes (honours cancellation).
internal sealed class TestHttpContent : HttpContent
{
    private long _pulled;
    private int _disposed;

    public byte[] Payload { get; init; } = [];

    public long? TotalLength { get; init; }

    public long? FailAfter { get; init; }

    public long? StallAfter { get; init; }

    public long BytesPulled => Interlocked.Read(ref _pulled);

    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, context, CancellationToken.None);
    }

    protected override async Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken)
    {
        using var source = new BodyStream(this);
        await source.CopyToAsync(stream, cancellationToken);
    }

    protected override Task<Stream> CreateContentReadStreamAsync()
    {
        return Task.FromResult<Stream>(new BodyStream(this));
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        Interlocked.Exchange(ref _disposed, 1);
        base.Dispose(disposing);
    }

    private sealed class BodyStream(TestHttpContent owner) : Stream
    {
        private long Length0 => owner.TotalLength ?? owner.Payload.Length;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var served = owner.BytesPulled;

            if (owner.FailAfter is { } failAt && served >= failAt)
            {
                throw new IOException("Simulated connection reset mid-body");
            }

            if (owner.StallAfter is { } stallAt && served >= stallAt)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            var remaining = Length0 - served;

            if (remaining <= 0 || owner.Payload.Length == 0)
            {
                return 0;
            }

            var limit = (int)Math.Min(buffer.Length, remaining);
            limit = (int)Math.Min(limit, (owner.FailAfter ?? long.MaxValue) - served);
            limit = (int)Math.Min(limit, (owner.StallAfter ?? long.MaxValue) - served);

            for (var i = 0; i < limit; i++)
            {
                buffer.Span[i] = owner.Payload[(served + i) % owner.Payload.Length];
            }

            Interlocked.Add(ref owner._pulled, limit);
            return limit;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
