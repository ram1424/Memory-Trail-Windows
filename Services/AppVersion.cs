using System.Reflection;
using System.Text.RegularExpressions;

namespace MemoryLane.Windows;

public static class AppVersion
{
    private static readonly Regex ReleasePattern = new(
        @"v?(?<major>\d+)(?:\.(?<minor>\d+))?(?:\.(?<patch>\d+))?(?:[-.]?build[.-]?(?<build>\d+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string InformationalVersion { get; } = ResolveInformationalVersion();
    public static string NormalizedVersion { get; } = Normalize(InformationalVersion);
    public static string ReleaseTag => $"v{NormalizedVersion}";
    public static string FriendlyVersion { get; } = FormatFriendly(NormalizedVersion);
    public static string BuildCommit { get; } = ResolveMetadata("BuildCommit");
    public static string DistributionChannel { get; } = ResolveMetadata("DistributionChannel");
    public static bool IsMicrosoftStoreBuild =>
        string.Equals(DistributionChannel, "MicrosoftStore", StringComparison.OrdinalIgnoreCase);

    public static int CompareReleaseTags(string left, string right)
    {
        var a = Parse(left);
        var b = Parse(right);

        var result = a.Major.CompareTo(b.Major);
        if (result != 0) return result;
        result = a.Minor.CompareTo(b.Minor);
        if (result != 0) return result;
        result = a.Patch.CompareTo(b.Patch);
        if (result != 0) return result;
        return a.Build.CompareTo(b.Build);
    }

    private static string ResolveMetadata(string key)
    {
        var metadata = Assembly.GetEntryAssembly()?
            .GetCustomAttributes<AssemblyMetadataAttribute>();

        return metadata?
            .FirstOrDefault(item =>
                string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            ?.Value ?? "";
    }

    private static string ResolveInformationalVersion()
    {
        var value = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(value))
            return "1.0.1";

        var plusIndex = value.IndexOf('+');
        return plusIndex >= 0 ? value[..plusIndex] : value;
    }

    private static string Normalize(string value)
    {
        var parsed = Parse(value);

        string baseVersion;
        if (parsed.Minor == 0 && parsed.Patch == 0)
            baseVersion = $"{parsed.Major}";
        else if (parsed.Patch == 0)
            baseVersion = $"{parsed.Major}.{parsed.Minor}";
        else
            baseVersion = $"{parsed.Major}.{parsed.Minor}.{parsed.Patch}";

        return parsed.Build > 0
            ? $"{baseVersion}-build.{parsed.Build}"
            : baseVersion;
    }

    private static string FormatFriendly(string value)
    {
        var parsed = Parse(value);

        string baseVersion;
        if (parsed.Minor == 0 && parsed.Patch == 0)
            baseVersion = $"{parsed.Major}";
        else if (parsed.Patch == 0)
            baseVersion = $"{parsed.Major}.{parsed.Minor}";
        else
            baseVersion = $"{parsed.Major}.{parsed.Minor}.{parsed.Patch}";

        return parsed.Build > 0
            ? $"Version {baseVersion} Build {parsed.Build}"
            : $"Version {baseVersion}";
    }

    private static (int Major, int Minor, int Patch, int Build) Parse(string value)
    {
        var match = ReleasePattern.Match(value ?? "");
        if (!match.Success)
            return (1, 0, 0, 0);

        static int Number(Group group)
            => int.TryParse(group.Value, out var number) ? number : 0;

        return (
            Number(match.Groups["major"]),
            Number(match.Groups["minor"]),
            Number(match.Groups["patch"]),
            Number(match.Groups["build"]));
    }
}
