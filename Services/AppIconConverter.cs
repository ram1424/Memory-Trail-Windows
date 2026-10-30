using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MemoryLane.Windows;

public sealed class AppIconConverter : IValueConverter
{
    public static ImageSource? ResolveAppIcon(string appName, string processName = "")
    {
        var item = new MemoryItem
        {
            SourceApp = appName,
            SourceProcess = processName
        };

        return LoadForItem(item) ?? LoadGenericIcon();
    }

    private static readonly Dictionary<string, ImageSource> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MemoryItem item)
            return null;

        var key = item.SourceExecutable ?? item.SourceProcess ?? item.SourceApp;
        if (!string.IsNullOrWhiteSpace(key) && Cache.TryGetValue(key, out var cached))
            return cached;

        var image = LoadForItem(item) ?? LoadGenericIcon();
        if (!string.IsNullOrWhiteSpace(key) && image is not null)
            Cache[key] = image;

        return image;
    }

    private static ImageSource? LoadForItem(MemoryItem item)
    {
        if (item.SourceApp.Equals("Memory Trail", StringComparison.OrdinalIgnoreCase) ||
            item.SourceApp.Equals("Memory Lane", StringComparison.OrdinalIgnoreCase))
        {
            try { return Branding.LoadImage() ?? LoadGenericIcon(); }
            catch { return LoadGenericIcon(); }
        }

        var path = item.SourceExecutable;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            path = ResolveExecutable(item.SourceProcess, item.SourceApp);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon is null) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(48, 48));
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveExecutable(string processName, string appName)
    {
        var process = (processName ?? "").Trim();
        var app = (appName ?? "").Trim();

        foreach (var name in CandidateProcessNames(process, app))
        {
            try
            {
                using var running = Process.GetProcessesByName(name).FirstOrDefault();
                var runningPath = running?.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(runningPath) && File.Exists(runningPath))
                    return runningPath;
            }
            catch { }
        }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var candidates = new List<string>();

        void Add(params string[] paths) => candidates.AddRange(paths);

        var key = (process + " " + app).ToLowerInvariant();

        if (key.Contains("explorer") || key.Contains("file explorer"))
            Add(Path.Combine(windows, "explorer.exe"));

        if (key.Contains("edge") || key.Contains("msedge"))
            Add(
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"));

        if (key.Contains("chrome"))
            Add(
                Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(local, "Google", "Chrome", "Application", "chrome.exe"));

        if (key.Contains("outlook"))
            Add(
                Path.Combine(programFiles, "Microsoft Office", "root", "Office16", "OUTLOOK.EXE"),
                Path.Combine(programFilesX86, "Microsoft Office", "root", "Office16", "OUTLOOK.EXE"),
                Path.Combine(local, "Microsoft", "Olk", "olk.exe"));

        if (key.Contains("teams"))
            Add(
                Path.Combine(local, "Microsoft", "WindowsApps", "ms-teams.exe"),
                Path.Combine(programFiles, "WindowsApps", "MSTeams.exe"));

        if (key.Contains("word"))
            Add(
                Path.Combine(programFiles, "Microsoft Office", "root", "Office16", "WINWORD.EXE"),
                Path.Combine(programFilesX86, "Microsoft Office", "root", "Office16", "WINWORD.EXE"));

        if (key.Contains("excel"))
            Add(
                Path.Combine(programFiles, "Microsoft Office", "root", "Office16", "EXCEL.EXE"),
                Path.Combine(programFilesX86, "Microsoft Office", "root", "Office16", "EXCEL.EXE"));

        if (key.Contains("powerpoint"))
            Add(
                Path.Combine(programFiles, "Microsoft Office", "root", "Office16", "POWERPNT.EXE"),
                Path.Combine(programFilesX86, "Microsoft Office", "root", "Office16", "POWERPNT.EXE"));

        if (key.Contains("onenote"))
            Add(
                Path.Combine(programFiles, "Microsoft Office", "root", "Office16", "ONENOTE.EXE"),
                Path.Combine(programFilesX86, "Microsoft Office", "root", "Office16", "ONENOTE.EXE"));

        return candidates.FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> CandidateProcessNames(string processName, string appName)
    {
        if (!string.IsNullOrWhiteSpace(processName))
            yield return Path.GetFileNameWithoutExtension(processName);

        var key = appName.ToLowerInvariant();

        if (key.Contains("edge")) yield return "msedge";
        if (key.Contains("chrome")) yield return "chrome";
        if (key.Contains("outlook")) { yield return "OUTLOOK"; yield return "olk"; }
        if (key.Contains("teams")) { yield return "ms-teams"; yield return "Teams"; }
        if (key.Contains("word")) yield return "WINWORD";
        if (key.Contains("excel")) yield return "EXCEL";
        if (key.Contains("powerpoint")) yield return "POWERPNT";
        if (key.Contains("onenote")) yield return "ONENOTE";
        if (key.Contains("explorer")) yield return "explorer";
    }

    private static ImageSource? LoadGenericIcon()
    {
        try
        {
            using var icon = System.Drawing.SystemIcons.Application;
            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(48, 48));
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
