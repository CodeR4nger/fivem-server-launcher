using System.Net;
using FiveMServerLauncher.Core.Enums;
using FiveMServerLauncher.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.Service;

public class CfxStatusServiceTests
{
    private static string ComponentJson(string id, string name, string status) =>
        $"{{\"id\":\"{id}\",\"name\":\"{name}\",\"status\":\"{status}\",\"group\":false}}";

    private static string SummaryJson(string fiveM, string redM, string indicator)
    {
        var fivem = ComponentJson("gh9dmv9xj3hk", "FiveM", fiveM);
        var redm = ComponentJson("ytm3dswd81gl", "RedM", redM);
        return $"{{\"components\":[{fivem},{redm}],\"status\":{{\"indicator\":\"{indicator}\",\"description\":\"d\"}}}}";
    }

    private static CfxStatusService CreateService(string json)
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        return new CfxStatusService(new HttpClient(handler));
    }

    [Fact]
    public async Task GetStatusesAsync_WhenComponentsOperational_ShouldReportOperationalForAll()
    {
        // Given
        var service = CreateService(SummaryJson("operational", "operational", "none"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.Operational, result[GameClient.FiveM]);
        Assert.Equal(CfxStatus.Operational, result[GameClient.RedM]);
        Assert.Equal(CfxStatus.Operational, result[GameClient.FiveMEnhanced]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenFiveMComponentDegraded_ShouldReportDegradedForFiveMOnly()
    {
        // Given
        var service = CreateService(SummaryJson("degraded_performance", "operational", "none"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.Degraded, result[GameClient.FiveM]);
        Assert.Equal(CfxStatus.Operational, result[GameClient.RedM]);
        Assert.Equal(CfxStatus.Operational, result[GameClient.FiveMEnhanced]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenRedMComponentMajorOutage_ShouldReportMajorOutageForRedM()
    {
        // Given
        var service = CreateService(SummaryJson("operational", "major_outage", "none"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.Operational, result[GameClient.FiveM]);
        Assert.Equal(CfxStatus.MajorOutage, result[GameClient.RedM]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenIndicatorMinor_ShouldMapEnhancedToDegraded()
    {
        // Given
        var service = CreateService(SummaryJson("operational", "operational", "minor"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.Degraded, result[GameClient.FiveMEnhanced]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenIndicatorMajor_ShouldMapEnhancedToMajorOutage()
    {
        // Given
        var service = CreateService(SummaryJson("operational", "operational", "major"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.MajorOutage, result[GameClient.FiveMEnhanced]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenIndicatorCritical_ShouldMapEnhancedToMajorOutage()
    {
        // Given
        var service = CreateService(SummaryJson("operational", "operational", "critical"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.MajorOutage, result[GameClient.FiveMEnhanced]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenComponentStatusUnknown_ShouldReportUnknown()
    {
        // Given
        var service = CreateService(SummaryJson("mystery_value", "operational", "none"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.Unknown, result[GameClient.FiveM]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenIndicatorUnknown_ShouldReportUnknownEnhanced()
    {
        // Given
        var service = CreateService(SummaryJson("operational", "operational", "mystery_value"));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.Unknown, result[GameClient.FiveMEnhanced]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenFiveMComponentMissing_ShouldReportUnknownFiveMWithoutThrowing()
    {
        // Given
        var json = $"{{\"components\":[{ComponentJson("ytm3dswd81gl", "RedM", "operational")}],\"status\":{{\"indicator\":\"none\",\"description\":\"d\"}}}}";
        var service = CreateService(json);

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.Unknown, result[GameClient.FiveM]);
        Assert.Equal(CfxStatus.Operational, result[GameClient.RedM]);
        Assert.Equal(CfxStatus.Operational, result[GameClient.FiveMEnhanced]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenComponentResolvedByNamedAlias_ShouldUseIdFallback()
    {
        // Given: FiveM component gone but a component keeps its id under a different name.
        var json = $"{{\"components\":[{ComponentJson("gh9dmv9xj3hk", "Renamed", "partial_outage")},{ComponentJson("ytm3dswd81gl", "RedM", "operational")}],\"status\":{{\"indicator\":\"none\",\"description\":\"d\"}}}}";
        var service = CreateService(json);

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.NotNull(result);
        Assert.Equal(CfxStatus.PartialOutage, result[GameClient.FiveM]);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenHttpError_ShouldReturnNull()
    {
        // Given
        var handler = new FakeHttpMessageHandler(throwOnSend: true);
        var service = new CfxStatusService(new HttpClient(handler));

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStatusesAsync_WhenMalformedJson_ShouldReturnNull()
    {
        // Given
        var service = CreateService("{not json");

        // When
        var result = await service.GetStatusesAsync();

        // Then
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStatusesAsync_ShouldOnlyMakeOneRequestPerCall()
    {
        // Given
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, SummaryJson("operational", "operational", "none"));
        var service = new CfxStatusService(new HttpClient(handler));

        // When
        await service.GetStatusesAsync();

        // Then
        Assert.Equal(1, handler.RequestCount);
    }
}