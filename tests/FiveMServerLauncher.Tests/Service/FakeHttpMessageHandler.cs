using System.Net;

namespace FiveMServerLauncher.Tests.Service;
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;
    private readonly byte[]? _contentBytes;
    private readonly bool _throwOnSend;

    public HttpRequestMessage? LastRequest { get; private set; }

    public int RequestCount { get; private set; }

    public FakeHttpMessageHandler(
        HttpStatusCode statusCode,
        string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    public FakeHttpMessageHandler(
        HttpStatusCode statusCode,
        byte[] content)
    {
        _statusCode = statusCode;
        _contentBytes = content;
        _content = string.Empty;
    }

    public FakeHttpMessageHandler(bool throwOnSend)
    {
        _throwOnSend = throwOnSend;
        _statusCode = HttpStatusCode.OK;
        _content = string.Empty;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_throwOnSend)
        {
            throw new HttpRequestException("Simulated network failure");
        }

        LastRequest = request;
        RequestCount++;
        var content = _contentBytes is not null
            ? new ByteArrayContent(_contentBytes)
            : new StringContent(_content);
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = content
        };

        return Task.FromResult(response);
    }
}
