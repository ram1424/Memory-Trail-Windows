using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryLane.Windows;

public sealed class UpdateCheckResult
{
    public bool CheckSucceeded { get; init; }
    public bool IsUpdateAvailable { get; init; }
    public bool IsSameVersionRefresh { get; init; }
    public string Message { get; init; } = "";
    public string ReleaseTag { get; init; } = "";
    public string ReleaseName { get; init; } = "";
    public string ReleaseNotes { get; init; } = "";
    public string ReleaseUrl { get; init; } = "";
    public string DownloadUrl { get; init; } = "";
    public string AssetDigest { get; init; } = "";
    public string ReleaseCommitSha { get; init; } = "";

    public string ReleaseIdentity =>
        string.IsNullOrWhiteSpace(ReleaseCommitSha)
            ? ReleaseTag
            : $"{ReleaseTag}@{ReleaseCommitSha}";
}

public sealed class StagedUpdateInfo
{
    public string ReleaseTag { get; init; } = "";
    public string ReleaseCommitSha { get; init; } = "";
    public string PayloadDirectory { get; init; } = "";
}

public static class UpdateService
{
    private const string LatestReleaseApi =
        "https://api.github.com/repos/ram1424/Memory-Trail-Windows/releases/latest";
    private const string ReleasesPage =
        "https://github.com/ram1424/Memory-Trail-Windows/releases";

    private static readonly HttpClient Client = CreateClient();
    private static bool _installScheduled;

    private static string UpdateRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Memory Trail",
        "Updates");

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateGitHubRequest(LatestReleaseApi);
        using var response = await Client.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return new UpdateCheckResult
            {
                Message =
                    "Memory Trail could not read the GitHub release feed. Check your internet connection and try again."
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

        var releaseCommit = await ResolveReleaseCommitAsync(release.TagName, cancellationToken);
        var versionComparison =
            AppVersion.CompareReleaseTags(release.TagName, AppVersion.ReleaseTag);

        var sameVersionRefresh =
            versionComparison == 0 &&
            !string.IsNullOrWhiteSpace(AppVersion.BuildCommit) &&
            !string.IsNullOrWhiteSpace(releaseCommit) &&
            !string.Equals(
                AppVersion.BuildCommit,
                releaseCommit,
                StringComparison.OrdinalIgnoreCase);

        var updateAvailable = versionComparison > 0 || sameVersionRefresh;

        return new UpdateCheckResult
        {
            CheckSucceeded = true,
            IsUpdateAvailable = updateAvailable,
            IsSameVersionRefresh = sameVersionRefresh,
            Message = updateAvailable
                ? "A newer Memory Trail release is available."
                : "Memory Trail is up to date.",
            ReleaseTag = release.TagName,
            ReleaseName = string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name,
            ReleaseNotes = release.Body ?? "",
            ReleaseUrl = release.HtmlUrl ?? ReleasesPage,
            DownloadUrl = asset?.BrowserDownloadUrl ?? "",
            AssetDigest = asset?.Digest ?? "",
            ReleaseCommitSha = releaseCommit
        };
    }

    public static async Task<StagedUpdateInfo> StageUpdateAsync(
        UpdateCheckResult result,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(result.DownloadUrl))
            throw new InvalidOperationException("The latest release does not include a downloadable Windows package.");

        var installDirectory = Path.GetDirectoryName(Environment.ProcessPath ?? "");
        if (string.IsNullOrWhiteSpace(installDirectory))
            throw new InvalidOperationException("Memory Trail could not determine its installation folder.");

        if (!CanWriteToDirectory(installDirectory))
        {
            throw new UnauthorizedAccessException(
                "Memory Trail cannot silently update this installation folder. " +
                "Move the app to a user-writable folder or install the update manually.");
        }

        Directory.CreateDirectory(UpdateRoot);

        var identity = SanitizePathPart(
            string.IsNullOrWhiteSpace(result.ReleaseCommitSha)
                ? result.ReleaseTag
                : $"{result.ReleaseTag}-{ShortSha(result.ReleaseCommitSha)}");

        var releaseDirectory = Path.Combine(UpdateRoot, identity);
        var payloadDirectory = Path.Combine(releaseDirectory, "payload");
        var zipPath = Path.Combine(releaseDirectory, "package.zip");

        if (Directory.Exists(releaseDirectory))
            Directory.Delete(releaseDirectory, recursive: true);

        Directory.CreateDirectory(releaseDirectory);

        using (var request = new HttpRequestMessage(HttpMethod.Get, result.DownloadUrl))
        using (var response = await Client.SendAsync(
                   request,
                   HttpCompletionOption.ResponseHeadersRead,
                   cancellationToken))
        {
            response.EnsureSuccessStatusCode();

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = File.Create(zipPath);
            await input.CopyToAsync(output, cancellationToken);
        }

        await VerifyDigestAsync(zipPath, result.AssetDigest, cancellationToken);

        Directory.CreateDirectory(payloadDirectory);
        ZipFile.ExtractToDirectory(zipPath, payloadDirectory, overwriteFiles: true);

        var executableName = Path.GetFileName(Environment.ProcessPath);
        var executable = Directory
            .EnumerateFiles(payloadDirectory, executableName, SearchOption.AllDirectories)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(executable))
            throw new InvalidDataException("The update package does not contain the Memory Trail executable.");

        var payloadRoot = Path.GetDirectoryName(executable)
            ?? throw new InvalidDataException("The update package has an invalid layout.");

        return new StagedUpdateInfo
        {
            ReleaseTag = result.ReleaseTag,
            ReleaseCommitSha = result.ReleaseCommitSha,
            PayloadDirectory = payloadRoot
        };
    }

    public static void ReconcilePendingUpdate(PreferencesService preferences)
    {
        if (string.IsNullOrWhiteSpace(preferences.PendingUpdateVersion))
            return;

        var sameInstalledBuild =
            !string.IsNullOrWhiteSpace(preferences.PendingUpdateReleaseCommit) &&
            !string.IsNullOrWhiteSpace(AppVersion.BuildCommit) &&
            string.Equals(
                preferences.PendingUpdateReleaseCommit,
                AppVersion.BuildCommit,
                StringComparison.OrdinalIgnoreCase);

        var currentIsNewer =
            AppVersion.CompareReleaseTags(
                AppVersion.ReleaseTag,
                preferences.PendingUpdateVersion) > 0;

        var payloadMissing =
            string.IsNullOrWhiteSpace(preferences.PendingUpdateDirectory) ||
            !Directory.Exists(preferences.PendingUpdateDirectory);

        if (sameInstalledBuild || currentIsNewer || payloadMissing)
        {
            ClearPendingUpdate(preferences);
            preferences.Save();
        }
    }

    public static bool SchedulePendingInstall(
        PreferencesService preferences,
        bool relaunchAfterInstall = false)
    {
        if (AppVersion.IsMicrosoftStoreBuild)
            return false;

        if (_installScheduled)
            return true;

        if (string.IsNullOrWhiteSpace(preferences.PendingUpdateDirectory) ||
            !Directory.Exists(preferences.PendingUpdateDirectory))
            return false;

        var processPath = Environment.ProcessPath ?? "";
        var installDirectory = Path.GetDirectoryName(processPath);
        if (string.IsNullOrWhiteSpace(processPath) ||
            string.IsNullOrWhiteSpace(installDirectory) ||
            !CanWriteToDirectory(installDirectory))
            return false;

        Directory.CreateDirectory(UpdateRoot);

        var scriptPath = Path.Combine(UpdateRoot, "install-pending-update.ps1");
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Memory Trail",
            "update-install.log");

        File.WriteAllText(scriptPath, BuildInstallerScript());

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-ProcessId");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
        startInfo.ArgumentList.Add("-Payload");
        startInfo.ArgumentList.Add(preferences.PendingUpdateDirectory);
        startInfo.ArgumentList.Add("-InstallDir");
        startInfo.ArgumentList.Add(installDirectory);
        startInfo.ArgumentList.Add("-LogPath");
        startInfo.ArgumentList.Add(logPath);
        startInfo.ArgumentList.Add("-RelaunchExe");
        startInfo.ArgumentList.Add(relaunchAfterInstall ? processPath : "");

        Process.Start(startInfo);
        _installScheduled = true;
        return true;
    }

    public static void OpenReleasesPage()
        => OpenUrl(ReleasesPage);

    public static void OpenDownload(UpdateCheckResult result)
        => OpenUrl(
            string.IsNullOrWhiteSpace(result.DownloadUrl)
                ? result.ReleaseUrl
                : result.DownloadUrl);

    public static void ClearPendingUpdate(PreferencesService preferences)
    {
        preferences.PendingUpdateVersion = "";
        preferences.PendingUpdateReleaseCommit = "";
        preferences.PendingUpdateDirectory = "";
    }

    private static async Task<string> ResolveReleaseCommitAsync(
        string tag,
        CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"https://api.github.com/repos/ram1424/Memory-Trail-Windows/git/ref/tags/{Uri.EscapeDataString(tag)}";

            using var request = CreateGitHubRequest(url);
            using var response = await Client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return "";

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var reference = JsonSerializer.Deserialize<GitRefResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return reference?.Object?.Sha ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static HttpRequestMessage CreateGitHubRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }

    private static async Task VerifyDigestAsync(
        string filePath,
        string digest,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(digest))
            return;

        const string prefix = "sha256:";
        if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var expected = digest[prefix.Length..].Trim();

        await using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var actualBytes = await sha.ComputeHashAsync(stream, cancellationToken);
        var actual = Convert.ToHexString(actualBytes).ToLowerInvariant();

        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded update failed SHA-256 verification.");
    }

    private static bool CanWriteToDirectory(string directory)
    {
        try
        {
            var probe = Path.Combine(
                directory,
                $".memory-trail-update-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "test");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizePathPart(string value)
        => string.Concat(value.Select(ch =>
            char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_'
                ? ch
                : '_'));

    private static string ShortSha(string sha)
        => sha.Length <= 12 ? sha : sha[..12];

    private static string BuildInstallerScript()
        => string.Join(
            Environment.NewLine,
            new[]
            {
                "param(",
                "  [Parameter(Mandatory=$true)][int]$ProcessId,",
                "  [Parameter(Mandatory=$true)][string]$Payload,",
                "  [Parameter(Mandatory=$true)][string]$InstallDir,",
                "  [Parameter(Mandatory=$true)][string]$LogPath,",
                "  [string]$RelaunchExe = ''",
                ")",
                "$ErrorActionPreference = 'Stop'",
                "try {",
                "  Wait-Process -Id $ProcessId -ErrorAction SilentlyContinue",
                "  Start-Sleep -Milliseconds 650",
                "  Get-ChildItem -LiteralPath $Payload -File -Recurse | ForEach-Object {",
                "    $relative = $_.FullName.Substring($Payload.Length).TrimStart('\\')",
                "    $destination = Join-Path $InstallDir $relative",
                "    $destinationFolder = Split-Path -Parent $destination",
                "    if ($destinationFolder) { New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null }",
                "    Copy-Item -LiteralPath $_.FullName -Destination $destination -Force",
                "  }",
                "  Add-Content -LiteralPath $LogPath -Value (('[{0:o}] Update installed successfully.' -f [DateTimeOffset]::Now))",
                "  if (-not [string]::IsNullOrWhiteSpace($RelaunchExe) -and (Test-Path -LiteralPath $RelaunchExe)) {",
                "    Start-Sleep -Milliseconds 350",
                "    Start-Process -FilePath $RelaunchExe -WorkingDirectory $InstallDir",
                "    Add-Content -LiteralPath $LogPath -Value (('[{0:o}] Updated Memory Trail relaunched automatically.' -f [DateTimeOffset]::Now))",
                "  }",
                "}",
                "catch {",
                "  Add-Content -LiteralPath $LogPath -Value (('[{0:o}] Update install failed: {1}' -f [DateTimeOffset]::Now, $_.Exception.Message))",
                "  exit 1",
                "}"
            });

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
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"Memory-Trail/{AppVersion.NormalizedVersion}");
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

        [JsonPropertyName("digest")]
        public string? Digest { get; set; }
    }

    private sealed class GitRefResponse
    {
        [JsonPropertyName("object")]
        public GitObject? Object { get; set; }
    }

    private sealed class GitObject
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = "";
    }
}
