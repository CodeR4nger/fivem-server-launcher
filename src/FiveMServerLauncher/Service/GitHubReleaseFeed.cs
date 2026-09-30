using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using FiveMServerLauncher.Core;

namespace FiveMServerLauncher.Service;

public interface IReleaseFeed
{
    Task<LauncherUpdate?> CheckForUpdateAsync();
}

public sealed class GitHubReleaseFeed : IReleaseFeed
{
    private const string LatestReleaseUrl =
        "https://api.github.com/repos/CodeR4nger/fivem-server-launcher/releases/latest";

    private const string AssetName = "CFXLauncher.exe";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public GitHubReleaseFeed(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LauncherUpdate?> CheckForUpdateAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
            request.Headers.UserAgent.ParseAdd($"{AppInfo.ProductName}/{AppInfo.Version}");
            using var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, SerializerOptions);

            if (release?.TagName is not { } tagName
                || AppVersion.TryParseTag(tagName) is not { } published
                || AppVersion.TryParseTag(AppInfo.Version) is not { } current
                || published <= current)
            {
                return null;
            }

            var asset = (release.Assets ?? []).FirstOrDefault(a => a.Name == AssetName);

            if (asset?.BrowserDownloadUrl is not { } downloadUrl)
            {
                return null;
            }

            return new LauncherUpdate(tagName, AssetName, asset.Size, downloadUrl);
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

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        public GitHubAsset[]? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        public string? Name { get; set; }

        public long Size { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
