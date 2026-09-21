using System.Net;
using System.Net.Http;
using System.Text.Json;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

public class CfxService(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
       PropertyNameCaseInsensitive = true
    };

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
            CfxId = cfxId,
            ProjectName = cfxResponse.Data.Sv_projectName ?? string.Empty,
            EnforceGameBuild = CfxVars.TryGetInt(cfxResponse.Data.Vars, "sv_enforceGameBuild"),
            GameClient = CfxVars.TryGetGameClient(cfxResponse.Data.Vars),
            PureLevel = CfxVars.TryGetInt(cfxResponse.Data.Vars, "sv_pureLevel"),
            RequestSteamTicket = CfxVars.MapSteamTicket(cfxResponse.Data.RequestSteamTicket),
            DefaultGameBuild = CfxVars.TryGetInt(cfxResponse.Data.Vars, "sv_defaultGameBuild"),
            ReplaceExecutableToSwitchBuilds = CfxVars.TryGetBool(cfxResponse.Data.Vars, "sv_replaceExeToSwitchBuilds"),
            PoolSizesIncrease = CfxVars.TryGetString(cfxResponse.Data.Vars, "sv_poolSizesIncrease"),
            SteamEnforced = CfxVars.TryGetBool(cfxResponse.Data.Vars, "sv_enforceSteamAuth")
        };
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
