using System.Net.Http;
using System.Text.Json;
using FiveMServerLauncher.Core.Enums;

namespace FiveMServerLauncher.Service;

public interface ICfxStatusService
{
    Task<IReadOnlyDictionary<GameClient, CfxStatus>?> GetStatusesAsync();
}

public sealed class CfxStatusService : ICfxStatusService
{
    private const string StatusUrl = "https://citizenfx.statuspage.io/api/v2/summary.json";
    private const string FiveMComponentId = "gh9dmv9xj3hk";
    private const string RedMComponentId = "ytm3dswd81gl";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public CfxStatusService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyDictionary<GameClient, CfxStatus>?> GetStatusesAsync()
    {
        try
        {
            var json = await _httpClient.GetStringAsync(StatusUrl);
            var parsed = JsonSerializer.Deserialize<StatusPageSummary>(json, SerializerOptions);

            if (parsed is null)
            {
                return null;
            }

            return ToStatuses(parsed);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<GameClient, CfxStatus> ToStatuses(StatusPageSummary summary)
    {
        var components = summary.Components ?? [];
        var fiveM = components.FirstOrDefault(c => c is { Name: "FiveM", Group: false })
            ?? components.FirstOrDefault(c => c.Id == FiveMComponentId);
        var redM = components.FirstOrDefault(c => c is { Name: "RedM", Group: false })
            ?? components.FirstOrDefault(c => c.Id == RedMComponentId);

        var indicator = summary.Status?.Indicator;

        return new Dictionary<GameClient, CfxStatus>
        {
            [GameClient.FiveM] = MapComponentStatus(fiveM?.Status),
            [GameClient.RedM] = MapComponentStatus(redM?.Status),
            [GameClient.FiveMEnhanced] = MapIndicator(indicator)
        };
    }

    internal static CfxStatus MapComponentStatus(string? status)
    {
        return status switch
        {
            "operational" => CfxStatus.Operational,
            "degraded_performance" => CfxStatus.Degraded,
            "partial_outage" => CfxStatus.PartialOutage,
            "major_outage" => CfxStatus.MajorOutage,
            "under_maintenance" => CfxStatus.Maintenance,
            _ => CfxStatus.Unknown
        };
    }

    internal static CfxStatus MapIndicator(string? indicator)
    {
        return indicator switch
        {
            "none" => CfxStatus.Operational,
            "minor" => CfxStatus.Degraded,
            "major" => CfxStatus.MajorOutage,
            "critical" => CfxStatus.MajorOutage,
            _ => CfxStatus.Unknown
        };
    }

    private sealed class StatusPageSummary
    {
        public StatusPageComponent[]? Components { get; set; }

        public StatusPageStatus? Status { get; set; }
    }

    private sealed class StatusPageStatus
    {
        public string? Indicator { get; set; }
    }

    private sealed class StatusPageComponent
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? Status { get; set; }

        public bool Group { get; set; }
    }
}