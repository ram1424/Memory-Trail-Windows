using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryLane.Windows;

public sealed class UpdateCheckResult
{
    public bool CheckSucceeded { get; init; }
    public bool IsUpdateAvailable { get; init; }
    public string Message { get; init; } = "";
    public string ReleaseTag { get; init; } = "";
    public string ReleaseName { get; init; } = "";
    public string ReleaseNotes { get; init; } = "";
    public string ReleaseUrl { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
}

public static class UpdateService
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/ram1424/Memory-Trail-Windows/releases/latest";
    private const string ReleasesPage =
        "https://github.com/ram1424/Memory-Trail-Windows/releases";

    private static readonly HttpClient Client = CreateClient();

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseApi);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

        var token = Environment.GetEnvironmentVariable("MEMORY_TRAIL_GITHUB_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await Client.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new UpdateCheckResult
            {
                Message =
                    "The GitHub release feed is not anonymously accessible. The Memory Trail repository is private, so automatic checks require release access. No timeline or captured activity was sent."
            };
        }

        if (!response.IsSuccessStatusCode)
        {
            return new UpdateCheckResult
            {
                Message = $"GitHub returned {(int)response.StatusCode} while checking for updates."
            };
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var release = JsonSerializer.Deserialize<GitHubRelease>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
        {
            return new UpdateCheckResult
            {
                Message = "The GitHub release feed did not contain a usable release."
            };
        }

        var asset = release.Assets?
            .FirstOrDefault(item =>
                item.Name.EndsWith("-x64.zip", StringComparison.OrdinalIgnoreCase))
            ?? release.Assets?.FirstOrDefault();

        var updateAvailable =
            AppVersion.CompareReleaseTags(release.TagName, AppVersion.ReleaseTag) > 0;

        return new UpdateCheckResult
        {
            CheckSucceeded = true,
            IsUpdateAvailable = updateAvailable,
            Message = updateAvailable ? "A newer Memory Trail release is available." : "Memory Trail is up to date.",
            ReleaseTag = release.TagName,
            ReleaseName = string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name,
            ReleaseNotes = release.Body ?? "",
            ReleaseUrl = release.HtmlUrl ?? ReleasesPage,
            DownloadUrl = asset?.BrowserDownloadUrl ?? release.HtmlUrl ?? ReleasesPage
        };
    }

    public static void OpenReleasesPage()
        => OpenUrl(ReleasesPage);

    public static void OpenDownload(UpdateCheckResult result)
        => OpenUrl(string.IsNullOrWhiteSpace(result.DownloadUrl) ? result.ReleaseUrl : result.DownloadUrl);

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Memory-Trail/{AppVersion.NormalizedVersion}");
        return client;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
