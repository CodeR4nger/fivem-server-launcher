using System.Net;

namespace FiveMServerLauncher.Tests.Service;
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    public HttpRequestMessage? LastRequest { get; private set; }

    private readonly string _content;

    public FakeHttpMessageHandler(
        HttpStatusCode statusCode,
        string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content)
        };

        return Task.FromResult(response);
    }
}
