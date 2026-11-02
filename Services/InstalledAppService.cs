using Microsoft.Win32;
using System.Diagnostics;

namespace MemoryLane.Windows;

public sealed record InstalledAppEntry(string DisplayName, string ProcessName, string? ExecutablePath)
{
    public string SearchText => $"{DisplayName} {ProcessName} {ExecutablePath}".ToLowerInvariant();
}

public static class InstalledAppService
{
    public static IReadOnlyList<InstalledAppEntry> Discover()
    {
        var apps = new Dictionary<string, InstalledAppEntry>(StringComparer.OrdinalIgnoreCase);

        ReadUninstallKey(RegistryHive.LocalMachine, RegistryView.Registry64, apps);
        ReadUninstallKey(RegistryHive.LocalMachine, RegistryView.Registry32, apps);
        ReadUninstallKey(RegistryHive.CurrentUser, RegistryView.Default, apps);
        ReadAppPaths(RegistryHive.LocalMachine, RegistryView.Registry64, apps);
        ReadAppPaths(RegistryHive.LocalMachine, RegistryView.Registry32, apps);
        ReadAppPaths(RegistryHive.CurrentUser, RegistryView.Default, apps);
        AddRunningApplications(apps);

        return apps.Values
            .Where(app => !string.Equals(app.ProcessName, "Memory Trail", StringComparison.OrdinalIgnoreCase))
            .OrderBy(app => app.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(app => app.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void ReadUninstallKey(
        RegistryHive hive,
        RegistryView view,
        IDictionary<string, InstalledAppEntry> apps)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var uninstall = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
            if (uninstall is null) return;

            foreach (var subkeyName in uninstall.GetSubKeyNames())
            {
                using var subkey = uninstall.OpenSubKey(subkeyName);
                if (subkey is null) continue;

                var name = subkey.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(name)) continue;

                var displayIcon = NormalizeExecutablePath(subkey.GetValue("DisplayIcon") as string);
                var installLocation = subkey.GetValue("InstallLocation") as string;
                var executable = displayIcon;

                if (string.IsNullOrWhiteSpace(executable) && !string.IsNullOrWhiteSpace(installLocation))
                    executable = FindLikelyExecutable(installLocation, name);

                var processName = GetProcessName(executable, name);
                if (string.IsNullOrWhiteSpace(processName)) continue;

                AddOrImprove(apps, new InstalledAppEntry(name.Trim(), processName, executable));
            }
        }
        catch
        {
            // Registry discovery is best effort. Missing or protected keys are expected.
        }
    }

    private static void ReadAppPaths(
        RegistryHive hive,
        RegistryView view,
        IDictionary<string, InstalledAppEntry> apps)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var appPaths = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");
            if (appPaths is null) return;

            foreach (var subkeyName in appPaths.GetSubKeyNames())
            {
                using var subkey = appPaths.OpenSubKey(subkeyName);
                var executable = NormalizeExecutablePath(subkey?.GetValue(null) as string);
                if (string.IsNullOrWhiteSpace(executable)) continue;

                var processName = Path.GetFileNameWithoutExtension(executable);
                if (string.IsNullOrWhiteSpace(processName)) continue;

                var displayName = FileVersionInfo.GetVersionInfo(executable).FileDescription;
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = processName;

                AddOrImprove(apps, new InstalledAppEntry(displayName.Trim(), processName, executable));
            }
        }
        catch
        {
            // App Paths discovery is best effort.
        }
    }

    private static void AddRunningApplications(IDictionary<string, InstalledAppEntry> apps)
    {
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var processName = process.ProcessName;
                if (string.IsNullOrWhiteSpace(processName)) continue;

                var path = process.MainModule?.FileName;
                var displayName = !string.IsNullOrWhiteSpace(path)
                    ? FileVersionInfo.GetVersionInfo(path).FileDescription
                    : null;

                AddOrImprove(
                    apps,
                    new InstalledAppEntry(
                        string.IsNullOrWhiteSpace(displayName) ? processName : displayName.Trim(),
                        processName,
                        path));
            }
            catch
            {
                // Protected processes cannot always expose their executable path.
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static void AddOrImprove(
        IDictionary<string, InstalledAppEntry> apps,
        InstalledAppEntry candidate)
    {
        var key = candidate.ProcessName.Trim();
        if (key.Length == 0) return;

        if (!apps.TryGetValue(key, out var current) ||
            (string.IsNullOrWhiteSpace(current.ExecutablePath) && !string.IsNullOrWhiteSpace(candidate.ExecutablePath)) ||
            current.DisplayName.Equals(current.ProcessName, StringComparison.OrdinalIgnoreCase))
        {
            apps[key] = candidate;
        }
    }

    private static string? NormalizeExecutablePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var cleaned = value.Trim().Trim('"');
        var comma = cleaned.LastIndexOf(',');
        if (comma > 2 && int.TryParse(cleaned[(comma + 1)..], out _))
            cleaned = cleaned[..comma];

        cleaned = Environment.ExpandEnvironmentVariables(cleaned.Trim().Trim('"'));
        return File.Exists(cleaned) ? cleaned : null;
    }

    private static string GetProcessName(string? executable, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(executable))
            return Path.GetFileNameWithoutExtension(executable);

        var sanitized = new string(fallbackName
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .ToArray());

        return sanitized;
    }

    private static string? FindLikelyExecutable(string directory, string displayName)
    {
        try
        {
            if (!Directory.Exists(directory)) return null;

            var normalizedName = new string(displayName.Where(char.IsLetterOrDigit).ToArray());
            return Directory
                .EnumerateFiles(directory, "*.exe", SearchOption.TopDirectoryOnly)
                .OrderByDescending(path =>
                {
                    var fileName = new string(Path.GetFileNameWithoutExtension(path).Where(char.IsLetterOrDigit).ToArray());
                    return fileName.Contains(normalizedName, StringComparison.OrdinalIgnoreCase) ||
                           normalizedName.Contains(fileName, StringComparison.OrdinalIgnoreCase);
                })
                .ThenBy(path => path.Length)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
