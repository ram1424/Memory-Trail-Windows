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

    public static ImageSource? ResolveAppIcon(string appName, string processName = "")
    {
        if (IsOutlook(appName, processName, ""))
            return CreateOutlookVectorIcon();

        var path = ResolveExecutable(processName, appName);
        return LoadExecutableIcon(path) ?? CreateBrandFallback(appName) ?? LoadGenericIcon();
    }

    // Tutorial artwork should be stable across machines. New Outlook can expose
    // package-backed or generic executable icons, so use our deterministic brand fallback.
    public static ImageSource? ResolveTutorialBrandIcon(string appName)
        => IsOutlook(appName, "", "")
            ? CreateOutlookVectorIcon()
            : CreateBrandFallback(appName) ?? LoadGenericIcon();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MemoryItem item)
            return null;

        var outlook = IsOutlook(item.SourceApp, item.SourceProcess, item.SourceExecutable);
        var key = outlook
            ? "brand::microsoft-outlook"
            : item.SourceExecutable ?? item.SourceProcess ?? item.SourceApp;

        if (!string.IsNullOrWhiteSpace(key) && Cache.TryGetValue(key, out var cached))
            return cached;

        ImageSource? image;
        if (item.SourceApp.Equals("Memory Trail", StringComparison.OrdinalIgnoreCase) ||
            item.SourceApp.Equals("Memory Lane", StringComparison.OrdinalIgnoreCase))
        {
            image = Branding.LoadImage();
        }
        else if (outlook)
        {
            // Outlook is rendered as a true WPF vector so Today/Newest First stays
            // sharp and stable regardless of classic/new Outlook packaging or DPI.
            image = CreateOutlookVectorIcon();
        }
        else
        {
            var path = item.SourceExecutable;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                path = ResolveExecutable(item.SourceProcess, item.SourceApp);

            image = LoadExecutableIcon(path) ?? CreateBrandFallback(item.SourceApp);
        }

        image ??= LoadGenericIcon();

        if (!string.IsNullOrWhiteSpace(key) && image is not null)
            Cache[key] = image;

        return image;
    }

    private static ImageSource? LoadExecutableIcon(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon is null)
                return null;

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

    private static bool IsOutlook(string? appName, string? processName, string? executable)
    {
        var process = Path.GetFileNameWithoutExtension(processName ?? "");
        var executableName = Path.GetFileNameWithoutExtension(executable ?? "");
        var combined = $"{appName} {process} {executableName}";

        return combined.Contains("outlook", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(process, "olk", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(executableName, "olk", StringComparison.OrdinalIgnoreCase);
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
                Path.Combine(local, "Microsoft", "Olk", "olk.exe"),
                Path.Combine(programFiles, "Microsoft Office", "root", "Office16", "OUTLOOK.EXE"),
                Path.Combine(programFilesX86, "Microsoft Office", "root", "Office16", "OUTLOOK.EXE"));

        if (key.Contains("teams"))
            Add(
                Path.Combine(local, "Microsoft", "WindowsApps", "ms-teams.exe"),
                Path.Combine(local, "Microsoft", "Teams", "current", "Teams.exe"));

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
        if (key.Contains("outlook")) { yield return "olk"; yield return "OUTLOOK"; }
        if (key.Contains("teams")) { yield return "ms-teams"; yield return "Teams"; }
        if (key.Contains("word")) yield return "WINWORD";
        if (key.Contains("excel")) yield return "EXCEL";
        if (key.Contains("powerpoint")) yield return "POWERPNT";
        if (key.Contains("onenote")) yield return "ONENOTE";
        if (key.Contains("explorer")) yield return "explorer";
    }

    private static ImageSource CreateOutlookVectorIcon()
    {
        var drawing = new DrawingGroup();

        // Outer Outlook tile.
        drawing.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(31, 111, 220)),
            null,
            new RectangleGeometry(new Rect(6, 6, 36, 36), 8, 8)));

        // Mail panel.
        drawing.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(45, 137, 239)),
            null,
            new RectangleGeometry(new Rect(18, 11, 25, 27), 5, 5)));

        // Envelope face.
        drawing.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(226, 241, 255)),
            null,
            new RectangleGeometry(new Rect(21, 15, 19, 15), 3, 3)));

        var envelopePen = new Pen(new SolidColorBrush(Color.FromRgb(62, 129, 214)), 1.7);
        drawing.Children.Add(new GeometryDrawing(
            null,
            envelopePen,
            Geometry.Parse("M21,16 L30.5,24 L40,16 M21,30 L27.5,23.5 M40,30 L33.5,23.5")));

        // Foreground Outlook 'O' tile.
        drawing.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(7, 82, 174)),
            null,
            new RectangleGeometry(new Rect(5, 10, 22, 28), 5, 5)));

        var glyph = new FormattedText(
            "O",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI Variable Display"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            18,
            Brushes.White,
            1.0);

        var glyphGeometry = glyph.BuildGeometry(new Point(
            16 - (glyph.Width / 2),
            24 - (glyph.Height / 2)));

        drawing.Children.Add(new GeometryDrawing(Brushes.White, null, glyphGeometry));

        drawing.Freeze();

        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static ImageSource? CreateBrandFallback(string appName)
    {
        var key = (appName ?? "").ToLowerInvariant();
        if (!(key.Contains("edge") || key.Contains("outlook") || key.Contains("teams") ||
              key.Contains("word") || key.Contains("explorer") || key.Contains("file explorer")))
            return null;

        const int logicalSize = 48;
        const int pixelSize = 192;
        const double scale = pixelSize / (double)logicalSize;
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale));

            if (key.Contains("edge"))
            {
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(12, 121, 209)), null, new Point(24, 24), 22, 22);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(22, 198, 174)), null, new Point(31, 17), 14, 14);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(10, 58, 105)), null, new Point(24, 24), 10, 10);
            }
            else if (key.Contains("outlook"))
            {
                dc.Pop();
                return CreateOutlookVectorIcon();
            }
            else if (key.Contains("teams"))
            {
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(98, 78, 216)), null, new Rect(6, 9, 36, 32), 8, 8);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(174, 159, 255)), null, new Point(39, 11), 5, 5);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(139, 119, 235)), null, new Point(40, 24), 5, 7);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(78, 59, 190)), null, new Rect(5, 11, 23, 27), 5, 5);
                DrawLetter(dc, "T", 6, 11, 21, 27, 18);
            }
            else if (key.Contains("word"))
            {
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(31, 105, 213)), null, new Rect(6, 6, 36, 36), 8, 8);
                DrawLetter(dc, "W", 6, 8, 36, 32, 19);
            }
            else
            {
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(244, 181, 42)), null, new Rect(5, 13, 38, 27), 6, 6);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 215, 82)), null, new Rect(6, 10, 18, 10), 4, 4);
            }

            dc.Pop();
        }

        var bitmap = new RenderTargetBitmap(pixelSize, pixelSize, 384, 384, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawLetter(DrawingContext dc, string text, double x, double y, double width, double height, double fontSize)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            fontSize,
            Brushes.White,
            1.0);

        var point = new Point(
            x + (width - formatted.Width) / 2,
            y + (height - formatted.Height) / 2);

        dc.DrawText(formatted, point);
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
