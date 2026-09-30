using System.Net.Http;
using System.Text.RegularExpressions;
using FiveMServerLauncher.Domain;

namespace FiveMServerLauncher.Service;

public interface IGameBuildDataService
{
    Task<GameBuildData?> FetchAsync();
}

public sealed class GameBuildDataService : IGameBuildDataService
{
    private const string PremakeUrl = "https://raw.githubusercontent.com/citizenfx/fivem/master/code/premake5_builds.lua";
    private const string ServersPageUrl = "https://servers.fivem.net/servers";
    private const string BundleBaseUrl = "https://servers.fivem.net";

    private static readonly Regex BundlePath = new("""src="(/assets/serversList-[^"]+\.js)""", RegexOptions.Compiled);
    private static readonly Regex SectionStart = new(@"^\s*(\w+)\s*=\s*\{", RegexOptions.Compiled);
    private static readonly Regex SectionEnd = new(@"^\s*\}", RegexOptions.Compiled);
    private static readonly Regex BuildEntry = new(@"^\s*game_(\d+)\s*=", RegexOptions.Compiled);
    private static readonly Regex BundleNameCase = new(@"case`(\d+)`|return`([^`]*)`", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;

    public GameBuildDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GameBuildData?> FetchAsync()
    {
        try
        {
            var lua = await _httpClient.GetStringAsync(PremakeUrl);
            var fiveM = ParsePremakeNumbers(lua, "five");
            var redM = ParsePremakeNumbers(lua, "rdr3");

            if (fiveM.Count == 0 || redM.Count == 0)
            {
                return null;
            }

            return new GameBuildData(fiveM, redM, await TryFetchNamesAsync() ?? GameBuilds.Baseline.Names);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyDictionary<int, string>?> TryFetchNamesAsync()
    {
        try
        {
            var html = await _httpClient.GetStringAsync(ServersPageUrl);
            var path = BundlePath.Match(html);
            if (!path.Success)
            {
                return null;
            }

            var bundle = await _httpClient.GetStringAsync(BundleBaseUrl + path.Groups[1].Value);
            var names = ParseBundleNames(bundle);
            if (names.Count == 0)
            {
                return null;
            }

            var merged = new Dictionary<int, string>(GameBuilds.Baseline.Names);
            foreach (var (build, name) in names)
            {
                merged[build] = name;
            }

            return merged;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private static List<int> ParsePremakeNumbers(string lua, string section)
    {
        var numbers = new List<int>();
        var inSection = false;

        foreach (var line in lua.Split('\n'))
        {
            if (inSection)
            {
                if (SectionEnd.IsMatch(line))
                {
                    inSection = false;
                }
                else if (BuildEntry.Match(line) is { Success: true } entry && int.TryParse(entry.Groups[1].Value, out var build))
                {
                    numbers.Add(build);
                }
            }
            else if (SectionStart.Match(line) is { Success: true } start && start.Groups[1].Value == section)
            {
                inSection = true;
            }
        }

        return numbers;
    }

    private static IReadOnlyDictionary<int, string> ParseBundleNames(string bundle)
    {
        var names = new Dictionary<int, string>();
        var functionIndex = bundle.IndexOf("getGameBuildDLCName", StringComparison.Ordinal);
        if (functionIndex < 0)
        {
            return names;
        }

        var endIndex = bundle.IndexOf("return``}", functionIndex, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return names;
        }

        var pendingCases = new List<int>();
        foreach (Match match in BundleNameCase.Matches(bundle[functionIndex..endIndex]))
        {
            if (match.Groups[1].Success)
            {
                if (int.TryParse(match.Groups[1].Value, out var build))
                {
                    pendingCases.Add(build);
                }
            }
            else if (match.Groups[2].Success)
            {
                var name = match.Groups[2].Value;
                if (name.Length > 0)
                {
                    foreach (var build in pendingCases)
                    {
                        names[build] = name;
                    }
                }

                pendingCases.Clear();
            }
        }

        return names;
    }
}
