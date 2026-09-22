using System.Net;

namespace FiveMServerLauncher.Tests.Service;

internal sealed class RoutedHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string PathPart, HttpStatusCode Status, byte[] Content, bool Throw)> _routes = new();
    private readonly List<(string PathPart, HttpStatusCode Status, string Content, bool Throw)> _textRoutes = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    public void AddBytesRoute(string pathPart, HttpStatusCode status, byte[] content)
    {
        _routes.Add((pathPart, status, content, Throw: false));
    }

    public void AddBytesRoute(string pathPart, bool throwOnSend)
    {
        _routes.Add((pathPart, HttpStatusCode.OK, Array.Empty<byte>(), throwOnSend));
    }

    public void AddTextRoute(string pathPart, HttpStatusCode status, string content)
    {
        _textRoutes.Add((pathPart, status, content, Throw: false));
    }

    public int RequestCountByPath(string pathPart)
    {
        return Requests.Count(r => r.RequestUri?.ToString().Contains(pathPart, StringComparison.OrdinalIgnoreCase) == true);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var uri = request.RequestUri?.ToString() ?? string.Empty;

        var byteRoute = _routes.LastOrDefault(r =>
            uri.Contains(r.PathPart, StringComparison.OrdinalIgnoreCase));
        if (byteRoute != default)
        {
            if (byteRoute.Throw)
            {
                throw new HttpRequestException("Simulated network failure");
            }

            return Task.FromResult(new HttpResponseMessage(byteRoute.Status)
            {
                Content = new ByteArrayContent(byteRoute.Content)
            });
        }

        var textRoute = _textRoutes.LastOrDefault(r =>
            uri.Contains(r.PathPart, StringComparison.OrdinalIgnoreCase));
        if (textRoute != default)
        {
            if (textRoute.Throw)
            {
                throw new HttpRequestException("Simulated network failure");
            }

            return Task.FromResult(new HttpResponseMessage(textRoute.Status)
            {
                Content = new StringContent(textRoute.Content)
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}