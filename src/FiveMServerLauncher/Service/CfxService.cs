using System.Net;
using System.Net.Http;
using System.Text.Json;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

public class CfxService(HttpClient httpClient)
{
    public const int MaxResponseBytes = 4 * 1024 * 1024;

    private readonly HttpClient _httpClient = httpClient;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
       PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Answers the server, or <c>null</c> ONLY when CFX authoritatively says the id is
    /// unknown (HTTP 404). Every other failure is an outage and throws in the
    /// sanctioned outage set (<see cref="BoundedContent.IsOutage"/>): any other
    /// non-success status, transport failures, timeouts, and an unusable or over-cap
    /// 200 body (an edge/proxy error page is not a verdict on the id).
    /// </summary>
    public async Task<CfxServerInfo?> GetServerAsync(string cfxId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"https://frontend.cfx-services.net/api/servers/single/{cfxId}");
        var body = await BoundedContent.GetBytesAsync(_httpClient, request, MaxResponseBytes);

        if (body.Status == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (body.Bytes is null)
        {
            throw new HttpRequestException(
                body.TooLarge ? "CFX response exceeded the size cap." : $"CFX answered {(int)body.Status}.",
                inner: null,
                body.TooLarge ? null : body.Status);
        }

        var data = TryParseData(body.Bytes)
            ?? throw new HttpRequestException("CFX answered an unusable server response.");

        return CfxVars.ToServerInfo(cfxId, data.Sv_projectName, data.Vars, data.RequestSteamTicket);
    }

    private static CfxServerData? TryParseData(byte[] json)
    {
        try
        {
            return JsonSerializer.Deserialize<CfxServerResponse>(json, SerializerOptions)?.Data;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class CfxServerResponse
    {
        public CfxServerData? Data { get; set; }
    }

    private sealed class CfxServerData
    {
        public string? Sv_projectName { get; set; }
        public string? RequestSteamTicket { get; set; }
        public Dictionary<string, string>? Vars { get; set; }

    }
}
