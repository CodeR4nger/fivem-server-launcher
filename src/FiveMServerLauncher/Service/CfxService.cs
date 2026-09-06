using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace FiveMServerLauncher.Service;

public class CfxService(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
       PropertyNameCaseInsensitive = true
    };
    private static int? GetIntVariable(
        Dictionary<string, string>? variables,
        string name)
    {
        if (variables is null ||
            !variables.TryGetValue(name, out var value))
        {
            return null;
        }

        return int.TryParse(value, out var result)
            ? result
            : null;
    }

    public async Task<CfxServerInfo?> GetServerAsync(string cfxId)
    {
        var response = await _httpClient.GetAsync($"https://frontend.cfx-services.net/api/servers/single/{cfxId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var cfxResponse = JsonSerializer.Deserialize<CfxServerResponse>(json,SerializerOptions);

        if (cfxResponse?.Data is null)
        {
            return null;
        }

        return new CfxServerInfo
        {
            CfxId = cfxResponse.EndPoint,
            ProjectName = cfxResponse.Data.Sv_projectName ?? string.Empty,
            EnforceGameBuild = GetIntVariable(cfxResponse.Data.Vars, "sv_enforceGameBuild"),
            PureLevel = GetIntVariable(cfxResponse.Data.Vars, "sv_pureLevel"),
            RequestSteamTicket = cfxResponse.Data.RequestSteamTicket == "on" ? true : null
        };
    }

    private sealed class CfxServerResponse
    {
        public CfxServerData? Data { get; set; }
        public required string EndPoint { get; set; }
    }

    private sealed class CfxServerData
    {
        public string? Sv_projectName { get; set; }
        public string? RequestSteamTicket { get; set; }
        public Dictionary<string, string>? Vars { get; set; }

    }
}
