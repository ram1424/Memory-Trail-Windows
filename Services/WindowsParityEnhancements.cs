using System.Collections.Specialized;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MemoryLane.Windows;

public static class WindowsParityEnhancements
{
    private static readonly TimeSpan ActiveSessionGap = TimeSpan.FromMinutes(5);

    public static void Attach(MainWindow window)
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background, window.Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(15)
        };

        void Refresh()
        {
            if (!window.Dispatcher.CheckAccess())
            {
                window.Dispatcher.BeginInvoke((Action)Refresh);
                return;
            }

            ApplyCurrentBrand(window);
            UpdateActiveTime(window);
            UpdateDynamicFilters(window);
        }

        NotifyCollectionChangedEventHandler collectionChanged = (_, _) => Refresh();
        App.Store.Items.CollectionChanged += collectionChanged;
        timer.Tick += (_, _) => Refresh();
        timer.Start();

        window.Loaded += (_, _) =>
        {
            AddInstalledAppsButton(window);
            HookAskResults(window);
            Refresh();
        };

        window.Closed += (_, _) =>
        {
            timer.Stop();
            App.Store.Items.CollectionChanged -= collectionChanged;
        };
    }

    private static void ApplyCurrentBrand(MainWindow window)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var label = version is null
            ? "1.0.0"
            : $"{version.Major}.{version.Minor}.{version.Build}";

        window.Title = $"Memory Trail v{label}";

        foreach (var text in FindVisualChildren<TextBlock>(window))
        {
            if (text.Text.StartsWith("v0.", StringComparison.OrdinalIgnoreCase) ||
                text.Text.StartsWith("v1.", StringComparison.OrdinalIgnoreCase))
            {
                text.Text = $"v{label}";
                break;
            }
        }
    }

    private static void UpdateActiveTime(MainWindow window)
    {
        var today = App.Store.Items
            .Where(item => item.CreatedAt.Date == DateTime.Today)
            .OrderBy(item => item.CreatedAt)
            .ToList();

        var active = TimeSpan.Zero;
        for (var index = 0; index + 1 < today.Count; index++)
        {
            var gap = today[index + 1].CreatedAt - today[index].CreatedAt;
            if (gap > TimeSpan.Zero && gap <= ActiveSessionGap)
                active += gap;
        }

        window.ActiveTimeText.Text = active switch
        {
            { TotalSeconds: <= 0 } => "0m",
            { TotalSeconds: < 60 } => "<1m",
            { TotalHours: >= 1 } => $"{(int)active.TotalHours}h {active.Minutes}m",
            _ => $"{Math.Max(1, active.Minutes)}m"
        };
    }

    private static void UpdateDynamicFilters(MainWindow window)
    {
        var memories = App.Store.Items.ToList();
        SetFilterVisibility(window.BrowserFilter, memories.Any(IsBrowser));
        SetFilterVisibility(window.EmailFilter, memories.Any(IsEmail));
        SetFilterVisibility(window.ChatFilter, memories.Any(IsChat));
        SetFilterVisibility(window.FilesFilter, memories.Any(IsFile));
        SetFilterVisibility(window.OfficeFilter, memories.Any(IsProductivity));
    }

    private static void SetFilterVisibility(Button button, bool hasData)
        => button.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;

    private static bool IsBrowser(MemoryItem item)
    {
        var source = $"{item.SourceApp} {item.SourceProcess}".ToLowerInvariant();
        return source.Contains("edge") || source.Contains("chrome") || source.Contains("firefox") ||
               source.Contains("brave") || source.Contains("opera") || source.Contains("vivaldi") ||
               source.Contains("browser");
    }

    private static bool IsEmail(MemoryItem item)
    {
        var source = $"{item.SourceApp} {item.SourceProcess}".ToLowerInvariant();
        return source.Contains("outlook") || source.Contains("mail") || source.Contains("thunderbird");
    }

    private static bool IsChat(MemoryItem item)
    {
        var source = $"{item.SourceApp} {item.SourceProcess}".ToLowerInvariant();
        return source.Contains("teams") || source.Contains("slack") || source.Contains("discord") ||
               source.Contains("zoom") || source.Contains("webex") || source.Contains("signal") ||
               source.Contains("whatsapp") || source.Contains("telegram") || source.Contains("chat");
    }

    private static bool IsFile(MemoryItem item)
    {
        var source = $"{item.SourceApp} {item.SourceProcess}".ToLowerInvariant();
        return item.SourceKind == SourceKind.File || source.Contains("explorer") ||
               source.Contains("word") || source.Contains("excel") || source.Contains("powerpoint") ||
               source.Contains("onenote") || source.Contains("notepad") || source.Contains("acrobat");
    }

    private static bool IsProductivity(MemoryItem item)
    {
        var source = $"{item.SourceApp} {item.SourceProcess}".ToLowerInvariant();
        return source.Contains("word") || source.Contains("excel") || source.Contains("powerpoint") ||
               source.Contains("onenote") || source.Contains("notepad") || source.Contains("calendar") ||
               source.Contains("todo") || source.Contains("project");
    }

    private static void AddInstalledAppsButton(MainWindow window)
    {
        if (window.ExclusionsBox.Parent is not StackPanel panel)
            return;

        if (panel.Children.OfType<Button>().Any(button => Equals(button.Tag, "installed-app-picker")))
            return;

        var button = new Button
        {
            Content = "Select installed apps",
            Tag = "installed-app-picker",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(14, 9, 14, 9),
            FontWeight = FontWeights.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(30, 120, 240)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(30, 120, 240)),
            ToolTip = "Choose from applications installed on this PC"
        };

        button.Click += (_, _) =>
        {
            var picker = new InstalledAppsWindow(App.Preferences) { Owner = window };
            if (picker.ShowDialog() != true) return;

            window.ExclusionsBox.Text = string.Join(
                Environment.NewLine,
                App.Preferences.ExcludedProcesses.OrderBy(value => value));
        };

        var index = panel.Children.IndexOf(window.ExclusionsBox);
        panel.Children.Insert(Math.Max(0, index), button);
    }

    private static void HookAskResults(MainWindow window)
    {
        window.AskResultsList.MouseDoubleClick += (_, eventArgs) =>
        {
            if (window.AskResultsList.SelectedItem is not MemoryItem item)
                return;

            var details = new MemoryDetailWindow(item) { Owner = window };
            details.ShowDialog();
            eventArgs.Handled = true;
        };

        window.AskResultsList.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.Key != Key.Enter || window.AskResultsList.SelectedItem is not MemoryItem item)
                return;

            var details = new MemoryDetailWindow(item) { Owner = window };
            details.ShowDialog();
            eventArgs.Handled = true;
        };
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                yield return match;

            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
