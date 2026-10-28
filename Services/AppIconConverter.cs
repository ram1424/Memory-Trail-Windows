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
        if (item.SourceApp.Equals("Memory Lane", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/Assets/MemoryLaneWindowsClockIcon.png");
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return LoadGenericIcon();
            }
        }

        var path = item.SourceExecutable;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            path = ResolveExecutable(item.SourceProcess);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon is null) return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveExecutable(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            return null;

        try
        {
            using var running = Process.GetProcessesByName(processName).FirstOrDefault();
            var runningPath = running?.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(runningPath) && File.Exists(runningPath))
                return runningPath;
        }
        catch { }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var candidates = processName.ToLowerInvariant() switch
        {
            "explorer" => new[] { Path.Combine(windows, "explorer.exe") },
            "msedge" => new[]
            {
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe")
            },
            "chrome" => new[]
            {
                Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe")
            },
            _ => Array.Empty<string>()
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static ImageSource? LoadGenericIcon()
    {
        try
        {
            using var icon = System.Drawing.SystemIcons.Application;
            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
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
