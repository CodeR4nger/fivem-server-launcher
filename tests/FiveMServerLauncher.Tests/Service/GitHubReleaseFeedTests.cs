using System.Net;
using FiveMServerLauncher.Core;
using FiveMServerLauncher.Service;
using Xunit;

namespace FiveMServerLauncher.Tests.Service;

public class GitHubReleaseFeedTests
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/CodeR4nger/fivem-server-launcher/releases/latest";

    private static string AssetJson(string tag) =>
        $$"""
        {
          "name": "CFXLauncher.exe",
          "size": 62000000,
          "browser_download_url": "https://github.com/CodeR4nger/fivem-server-launcher/releases/download/{{tag}}/CFXLauncher.exe"
        }
        """;

    private static string ReleaseJson(string tag, string? assets = null) =>
        $$"""
        {
          "tag_name": "{{tag}}",
          "assets": [{{assets ?? AssetJson(tag)}}]
        }
        """;

    private static string NewerTag()
    {
        var current = AppVersion.TryParseTag(AppInfo.Version)!;
        return $"v{current.Major + 1}.0.0";
    }

    private static GitHubReleaseFeed CreateFeed(out FakeHttpMessageHandler handler, HttpStatusCode status, string content)
    {
        handler = new FakeHttpMessageHandler(status, content);
        return new GitHubReleaseFeed(new HttpClient(handler));
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenNewerReleasePublished_ShouldReturnUpdateWithAssetFacts()
    {
        // Given
        var tag = NewerTag();
        var feed = CreateFeed(out var handler, HttpStatusCode.OK, ReleaseJson(tag));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then: the update carries the release version and its portable-exe asset facts
        Assert.NotNull(update);
        Assert.Equal(tag, update.Tag);
        Assert.Equal("CFXLauncher.exe", update.AssetName);
        Assert.Equal(62_000_000L, update.AssetSize);
        Assert.Equal(
            $"https://github.com/CodeR4nger/fivem-server-launcher/releases/download/{tag}/CFXLauncher.exe",
            update.DownloadUrl);

        // And: the request hit the documented latest-release endpoint identifying itself
        // (the GitHub API rejects requests without a User-Agent)
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(LatestReleaseUrl, handler.LastRequest.RequestUri?.ToString());
        Assert.False(string.IsNullOrEmpty(handler.LastRequest.Headers.UserAgent.ToString()));
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenSameVersionPublished_ShouldReturnNull()
    {
        // Given: the latest release carries exactly the running version
        var feed = CreateFeed(out _, HttpStatusCode.OK, ReleaseJson(AppInfo.Version));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenOlderVersionPublished_ShouldReturnNull()
    {
        // Given: the latest release is older than the running version (never downgrade)
        var feed = CreateFeed(out _, HttpStatusCode.OK, ReleaseJson("v1.0.0"));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenGarbageTagPublished_ShouldReturnNull()
    {
        // Given: the latest release carries no parseable version (never install anything
        // untagged)
        var feed = CreateFeed(out _, HttpStatusCode.OK, ReleaseJson("nightly-20260930"));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenReleaseCarriesDecoyAssets_ShouldPickPortableExe()
    {
        // Given: a real release carries several assets; only one is the portable exe
        var tag = NewerTag();
        var feed = CreateFeed(out _, HttpStatusCode.OK, ReleaseJson(tag, $$"""
            {
              "name": "CFXLauncher.exe.sha256",
              "size": 64,
              "browser_download_url": "https://example.invalid/CFXLauncher.exe.sha256"
            },
            {
              "name": "CFXLauncher.exe",
              "size": 62000000,
              "browser_download_url": "https://github.com/CodeR4nger/fivem-server-launcher/releases/download/{{tag}}/CFXLauncher.exe"
            }
            """));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.NotNull(update);
        Assert.Equal("CFXLauncher.exe", update.AssetName);
        Assert.Equal(62_000_000L, update.AssetSize);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenAssetMisnamed_ShouldReturnNull()
    {
        // Given: the newer release ships no CFXLauncher.exe asset (misnamed upload)
        var feed = CreateFeed(out _, HttpStatusCode.OK, ReleaseJson(NewerTag(), """
            {
              "name": "CFXLauncher.zip",
              "size": 62000000,
              "browser_download_url": "https://example.invalid/CFXLauncher.zip"
            }
            """));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenReleaseHasNoAssets_ShouldReturnNull()
    {
        // Given
        var feed = CreateFeed(out _, HttpStatusCode.OK, ReleaseJson(NewerTag(), ""));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenNoReleasesExist_ShouldReturnNull()
    {
        // Given: GitHub answers 404 with its error body when no published release exists
        var feed = CreateFeed(out _, HttpStatusCode.NotFound, """
            {
              "message": "Not Found",
              "documentation_url": "https://docs.github.com/rest/releases/releases#get-the-latest-release"
            }
            """);

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenBodyIsMalformed_ShouldReturnNull()
    {
        // Given
        var feed = CreateFeed(out _, HttpStatusCode.OK, "<html>maintenance</html>");

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenNetworkFails_ShouldReturnNull()
    {
        // Given: an outage (offline, DNS, GitHub down) never reaches the caller
        var handler = new FakeHttpMessageHandler(true);
        var feed = new GitHubReleaseFeed(new HttpClient(handler));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenRequestTimesOut_ShouldReturnNull()
    {
        // Given: the shared client's timeout surfaces as TaskCanceledException
        var feed = new GitHubReleaseFeed(new HttpClient(new ThrowingHandler(new TaskCanceledException())));

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenRateLimited_ShouldReturnNull()
    {
        // Given: the anonymous API answers 403 with its rate-limit body
        var feed = CreateFeed(out _, HttpStatusCode.Forbidden, """
            {
              "message": "API rate limit exceeded for 203.0.113.7.",
              "documentation_url": "https://docs.github.com/rest/overview/resources-in-the-rest-api#rate-limiting"
            }
            """);

        // When
        var update = await feed.CheckForUpdateAsync();

        // Then
        Assert.Null(update);
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw exception;
        }
    }
}
