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

            // An absent digest falls back to the applier's size check; a published one that
            // cannot be verified against is no update at all, never a silent downgrade.
            string? sha256 = null;

            if (asset.Digest is { } digest && !TryParseSha256Digest(digest, out sha256))
            {
                return null;
            }

            return new LauncherUpdate(tagName, AssetName, asset.Size, downloadUrl, sha256);
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

    // GitHub publishes asset digests as "sha256:<64 hex>".
    private static bool TryParseSha256Digest(string digest, out string? sha256)
    {
        const string prefix = "sha256:";
        const int hexLength = 64;
        sha256 = null;

        if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hex = digest[prefix.Length..];

        if (hex.Length != hexLength || !hex.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        sha256 = hex.ToLowerInvariant();
        return true;
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

        public string? Digest { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
