using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MemoryLane.Windows;

internal static class ScreenshotExporter
{
    private const int MainWidth = 1560;
    private const int MainHeight = 860;
    private const int OnboardingWidth = 920;
    private const int OnboardingHeight = 680;
    private const double RenderScale = 2.0;

    public static void ExportAll(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        SeedDemoTimeline();

        var main = new MainWindow();

        RenderWindow(main, Path.Combine(outputDirectory, "today-ui.png"), MainWidth, MainHeight);

        main.AskMemoryTrailNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        main.AskMemoryTrailBox.Text = "What conversations did I have today?";
        main.AskMemoryTrailButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        RenderWindow(main, Path.Combine(outputDirectory, "ask-memory-trail-ui.png"), MainWidth, MainHeight);

        main.SettingsNavButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        PrepareWindow(main, MainWidth, MainHeight);
        main.SettingsPanel.ScrollToVerticalOffset(150);
        main.SettingsPanel.UpdateLayout();
        RenderWindow(main, Path.Combine(outputDirectory, "privacy-settings-ui.png"), MainWidth, MainHeight);

        var onboarding = new OnboardingWindow();
        for (var i = 0; i < 4; i++)
            onboarding.NextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        RenderWindow(
            onboarding,
            Path.Combine(outputDirectory, "onboarding-privacy-ui.png"),
            OnboardingWidth,
            OnboardingHeight);
    }

    private static void SeedDemoTimeline()
    {
        App.Store.Items.Clear();

        var now = DateTime.Now;
        var items = new[]
        {
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-2),
                Title = "Memory Trail · GitHub",
                Body = "README and release notes",
                SourceApp = "Microsoft Edge",
                SourceProcess = "msedge",
                SourceKind = SourceKind.AppActivity,
                SourceUrl = "https://github.com"
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-11),
                Title = "Weekly planning update",
                SourceApp = "Outlook",
                SourceProcess = "olk",
                SourceKind = SourceKind.AppActivity,
                IsFavorite = true
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-19),
                Title = "Project Atlas | Team chat",
                SourceApp = "Microsoft Teams",
                SourceProcess = "ms-teams",
                SourceKind = SourceKind.AppActivity
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-31),
                Title = "Budget Forecast.xlsx",
                SourceApp = "Microsoft Excel",
                SourceProcess = "excel",
                SourceKind = SourceKind.File
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-43),
                Title = "Launch Plan.docx",
                SourceApp = "Microsoft Word",
                SourceProcess = "winword",
                SourceKind = SourceKind.File
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-56),
                Title = "Screenshots",
                SourceApp = "File Explorer",
                SourceProcess = "explorer",
                SourceKind = SourceKind.File
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-71),
                Title = "Design review | product-ui",
                SourceApp = "Slack",
                SourceProcess = "slack",
                SourceKind = SourceKind.AppActivity
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-86),
                Title = "MemoryTrail.Windows.csproj",
                SourceApp = "Visual Studio",
                SourceProcess = "devenv",
                SourceKind = SourceKind.File
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-101),
                Title = "Release checklist",
                Body = "Copied text",
                SourceApp = "Clipboard",
                SourceProcess = "Memory Trail",
                SourceKind = SourceKind.Clipboard
            },
            new MemoryItem
            {
                CreatedAt = now.AddMinutes(-118),
                Title = "Product Roadmap.pptx",
                SourceApp = "Microsoft PowerPoint",
                SourceProcess = "powerpnt",
                SourceKind = SourceKind.File
            }
        };

        foreach (var item in items)
            App.Store.Items.Add(item);
    }

    private static void RenderWindow(Window window, string path, int logicalWidth, int logicalHeight)
    {
        PrepareWindow(window, logicalWidth, logicalHeight);

        if (window.Content is not FrameworkElement root)
            throw new InvalidOperationException("Window content is not a FrameworkElement.");

        window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

        var bitmap = new RenderTargetBitmap(
            (int)Math.Round(logicalWidth * RenderScale),
            (int)Math.Round(logicalHeight * RenderScale),
            96 * RenderScale,
            96 * RenderScale,
            PixelFormats.Pbgra32);

        bitmap.Render(root);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void PrepareWindow(Window window, int logicalWidth, int logicalHeight)
    {
        window.Width = logicalWidth;
        window.Height = logicalHeight;

        if (window.Content is not FrameworkElement root)
            throw new InvalidOperationException("Window content is not a FrameworkElement.");

        root.Width = logicalWidth;
        root.Height = logicalHeight;
        root.Measure(new Size(logicalWidth, logicalHeight));
        root.Arrange(new Rect(0, 0, logicalWidth, logicalHeight));
        root.UpdateLayout();
    }
}
