using System.ComponentModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace MemoryLane.Windows;

public partial class MainWindow : Window
{
    private readonly ICollectionView _view;
    private Forms.NotifyIcon? _trayIcon;
    private bool _quitting;
    private string _mode = "Timeline";
    private string _category = "All";
    private bool _initializingSettings = true;
    private bool _detailsPaneVisible = true;
    private bool _rightSidebarVisible = true;

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            var brand = Branding.LoadImage();
            if (brand is not null)
            {
                Icon = brand;
                BrandLogo.Source = brand;
            }
        }
        catch (Exception ex)
        {
            App.LogNonFatal("MainWindow branding", ex);
        }

        _view = CollectionViewSource.GetDefaultView(App.Store.Items);
        _view.Filter = FilterItem;
        _view.SortDescriptions.Clear();
        _view.SortDescriptions.Add(new SortDescription(nameof(MemoryItem.CreatedAt), ListSortDirection.Descending));
        TimelineList.ItemsSource = _view;
        App.Store.Items.CollectionChanged += StoreItems_CollectionChanged;

        LoadSettingsIntoUi();
        UpdateCaptureState();
        UpdateNavSelection("Timeline");
        UpdateCategorySelection("All");

        if (App.Store.Items.Count > 0)
            TimelineList.SelectedIndex = 0;

        UpdateTodaySummary();
        UpdateLiveActivity();
        ApplyRightSidebarVisibility();

        try
        {
            _trayIcon = new Forms.NotifyIcon
            {
                Text = "Memory Trail",
                Visible = true,
                Icon = CreateTrayIcon()
            };

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Open Memory Trail", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
            menu.Items.Add("Pause / Resume Capture", null, (_, _) => Dispatcher.Invoke(ToggleCapture));
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(QuitApplication));
            _trayIcon.ContextMenuStrip = menu;
            _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Tray initialization", ex);
            _trayIcon = null;
        }

        Closing += OnClosing;
        Loaded += MainWindow_Loaded;

        _initializingSettings = false;
    }

    private static System.Drawing.Icon CreateTrayIcon()
    {
        try
        {
            return Branding.CreateTrayIcon();
        }
        catch (Exception ex)
        {
            App.LogNonFatal("CreateTrayIcon", ex);
            return System.Drawing.SystemIcons.Application;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        if (!App.Preferences.HasCompletedOnboarding)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try { ShowOnboarding(); }
                catch (Exception ex)
                {
                    App.LogNonFatal("Onboarding", ex);
                    MessageBox.Show(
                        "Memory Trail started successfully, but the welcome tutorial could not be opened. You can retry it later from Settings.",
                        "Memory Trail",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }));
        }
    }

    private void LoadSettingsIntoUi()
    {
        PauseCaptureCheckBox.IsChecked = App.Preferences.CapturePaused;
        AppCaptureCheckBox.IsChecked = App.Preferences.CaptureAppActivity;
        ClipboardCaptureCheckBox.IsChecked = App.Preferences.CaptureClipboard;
        StartWithWindowsCheckBox.IsChecked = App.Preferences.StartWithWindows;
        ExclusionsBox.Text = string.Join(Environment.NewLine, App.Preferences.ExcludedProcesses.OrderBy(x => x));
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_quitting) return;
        e.Cancel = true;
        Hide();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void QuitApplication()
    {
        _quitting = true;
        if (_trayIcon is not null)
            _trayIcon.Visible = false;
        Application.Current.Shutdown();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string mode) return;

        _mode = mode;
        UpdateNavSelection(mode);

        var showSettings = mode == "Settings";
        var showPrivacy = mode == "Privacy";
        var showAsk = mode == "Ask";

        ActivityShell.Visibility = showSettings || showPrivacy || showAsk ? Visibility.Collapsed : Visibility.Visible;
        AskMemoryTrailPanel.Visibility = showAsk ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
        PrivacyPanel.Visibility = showPrivacy ? Visibility.Visible : Visibility.Collapsed;

        _view.Refresh();
        UpdateLiveActivity();

        if (showAsk)
            Dispatcher.BeginInvoke(new Action(() => AskMemoryTrailBox.Focus()));
    }

    private void UpdateNavSelection(string mode)
    {
        var buttons = new[]
        {
            (Button: TimelineNavButton, Mode: "Timeline"),
            (Button: AskMemoryTrailNavButton, Mode: "Ask"),
            (Button: ClipboardNavButton, Mode: "Clipboard"),
            (Button: FavoritesNavButton, Mode: "Favorites"),
            (Button: NotesNavButton, Mode: "Notes"),
            (Button: PrivacyNavButton, Mode: "Privacy"),
            (Button: SettingsNavButton, Mode: "Settings")
        };

        foreach (var entry in buttons)
        {
            var selected = entry.Mode == mode;
            entry.Button.Background = selected
                ? new SolidColorBrush(Color.FromRgb(214, 231, 255))
                : Brushes.Transparent;
            entry.Button.Foreground = selected
                ? new SolidColorBrush(Color.FromRgb(27, 105, 220))
                : new SolidColorBrush(Color.FromRgb(52, 69, 94));
            entry.Button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string category)
            return;

        _category = category;
        UpdateCategorySelection(category);
        _view.Refresh();
        UpdateLiveActivity();
    }

    private void UpdateCategorySelection(string category)
    {
        var buttons = new[]
        {
            (Button: AllAppsFilter, Category: "All"),
            (Button: BrowserFilter, Category: "Browser"),
            (Button: EmailFilter, Category: "Email"),
            (Button: ChatFilter, Category: "Chat"),
            (Button: FilesFilter, Category: "Files"),
            (Button: OfficeFilter, Category: "Office")
        };

        foreach (var entry in buttons)
        {
            var selected = entry.Category == category;
            entry.Button.Background = new SolidColorBrush(selected
                ? Color.FromRgb(220, 235, 255)
                : Color.FromRgb(244, 248, 253));
            entry.Button.BorderBrush = new SolidColorBrush(selected
                ? Color.FromRgb(161, 199, 243)
                : Color.FromRgb(216, 226, 239));
            entry.Button.Foreground = selected
                ? new SolidColorBrush(Color.FromRgb(25, 79, 146))
                : new SolidColorBrush(Color.FromRgb(83, 100, 122));
        }
    }

    private bool FilterItem(object item)
    {
        if (item is not MemoryItem memory) return false;

        if (_mode == "Favorites" && !memory.IsFavorite)
            return false;
        if (_mode == "Clipboard" && memory.SourceKind != SourceKind.Clipboard)
            return false;
        if (_mode == "Notes" && memory.SourceKind != SourceKind.Note)
            return false;

        if (_category != "All" && !MatchesCategory(memory, _category))
            return false;

        return true;
    }

    private static bool MatchesCategory(MemoryItem memory, string category)
    {
        var app = (memory.SourceApp ?? "").ToLowerInvariant();

        return category switch
        {
            "Browser" => app.Contains("edge") || app.Contains("chrome") || app.Contains("firefox") ||
                         app.Contains("browser") || !string.IsNullOrWhiteSpace(memory.SourceUrl),
            "Email" => app.Contains("outlook") || app.Contains("mail"),
            "Chat" => app.Contains("teams") || app.Contains("slack") || app.Contains("discord") || app.Contains("chat"),
            "Files" => memory.SourceKind == SourceKind.File || app.Contains("explorer") || app.Contains("finder"),
            "Office" => app.Contains("word") || app.Contains("excel") || app.Contains("powerpoint") ||
                        app.Contains("onenote") || app.Contains("outlook"),
            _ => true
        };
    }

    private void AskSuggestion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string prompt })
            return;

        AskMemoryTrailBox.Text = prompt;
        RunAskMemoryTrail(prompt);
    }

    private void AskMemoryTrail_Click(object sender, RoutedEventArgs e)
        => RunAskMemoryTrail(AskMemoryTrailBox.Text);

    private void AskMemoryTrailBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        RunAskMemoryTrail(AskMemoryTrailBox.Text);
        e.Handled = true;
    }

    private void RunAskMemoryTrail(string? rawQuery)
    {
        var query = rawQuery?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(query))
        {
            AskEmptyState.Visibility = Visibility.Visible;
            AskResultsState.Visibility = Visibility.Collapsed;
            return;
        }

        var ordered = App.Store.Items
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var lowered = query.ToLowerInvariant();
        List<MemoryItem> results;
        string answer;

        if (lowered.Contains("before") && lowered.Contains("teams"))
        {
            var teamsIndex = ordered.FindIndex(x =>
                (x.SourceApp ?? "").Contains("Teams", StringComparison.OrdinalIgnoreCase));

            if (teamsIndex >= 0 && teamsIndex + 1 < ordered.Count)
            {
                results = new List<MemoryItem> { ordered[teamsIndex + 1] };
                var item = results[0];
                answer = $"Immediately before Teams, you were in {item.SourceApp}: {item.Title}.";
            }
            else
            {
                results = new List<MemoryItem>();
                answer = "I couldn't find an activity immediately before Teams in the captured timeline.";
            }
        }
        else if (lowered.Contains("yesterday") && lowered.Contains("file"))
        {
            var day = DateTime.Today.AddDays(-1);
            results = ordered
                .Where(x => x.CreatedAt.Date == day &&
                            (x.SourceKind == SourceKind.File ||
                             (x.SourceApp ?? "").Contains("Explorer", StringComparison.OrdinalIgnoreCase) ||
                             !string.IsNullOrWhiteSpace(x.SourceExecutable)))
                .Take(20)
                .ToList();
            answer = results.Count == 0
                ? "I couldn't find a captured file activity from yesterday."
                : $"I found {results.Count} file-related activit{(results.Count == 1 ? "y" : "ies")} from yesterday.";
        }
        else if (lowered.Contains("email"))
        {
            results = ordered
                .Where(x => (x.SourceApp ?? "").Contains("Outlook", StringComparison.OrdinalIgnoreCase) ||
                            (x.SourceApp ?? "").Contains("Mail", StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .ToList();
            answer = results.Count == 0
                ? "I couldn't find recent captured email activity."
                : $"I found {results.Count} recent email activit{(results.Count == 1 ? "y" : "ies")}.";
        }
        else if (lowered.Contains("conversation") || lowered.Contains("chat"))
        {
            results = ordered
                .Where(x => (x.SourceApp ?? "").Contains("Teams", StringComparison.OrdinalIgnoreCase) ||
                            (x.SourceApp ?? "").Contains("Slack", StringComparison.OrdinalIgnoreCase) ||
                            (x.SourceApp ?? "").Contains("Chat", StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .ToList();
            answer = results.Count == 0
                ? "I couldn't find recent captured conversation activity."
                : $"I found {results.Count} recent conversation activit{(results.Count == 1 ? "y" : "ies")}.";
        }
        else if (lowered.Contains("website") || lowered.Contains("websites") || lowered.Contains("browser"))
        {
            results = ordered
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceUrl) ||
                            (x.SourceApp ?? "").Contains("Edge", StringComparison.OrdinalIgnoreCase) ||
                            (x.SourceApp ?? "").Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
                            (x.SourceApp ?? "").Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .ToList();
            answer = results.Count == 0
                ? "I couldn't find recent captured website activity."
                : $"I found {results.Count} recent web activit{(results.Count == 1 ? "y" : "ies")}.";
        }
        else if (lowered.Contains("file") && (lowered.Contains("recent") || lowered.Contains("recently")))
        {
            results = ordered
                .Where(x => x.SourceKind == SourceKind.File ||
                            (x.SourceApp ?? "").Contains("Explorer", StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .ToList();
            answer = results.Count == 0
                ? "I couldn't find recent captured file activity."
                : $"I found {results.Count} recent file activit{(results.Count == 1 ? "y" : "ies")}.";
        }
        else if (lowered.Contains("this week"))
        {
            var start = DateTime.Today.AddDays(-6);
            results = ordered
                .Where(x => x.CreatedAt.Date >= start)
                .Take(40)
                .ToList();
            answer = results.Count == 0
                ? "I couldn't find captured activity from this week."
                : $"Here are your {results.Count} most recent activities from this week.";
        }
        else if (lowered.Contains("most recent") || lowered.Contains("recent activity"))
        {
            results = ordered.Take(25).ToList();
            answer = results.Count == 0
                ? "No activity has been captured yet."
                : "Here is your most recent captured activity.";
        }
        else if (lowered.Contains("today") || lowered.Contains("earlier today"))
        {
            results = ordered
                .Where(x => x.CreatedAt.Date == DateTime.Today)
                .Take(30)
                .ToList();
            answer = results.Count == 0
                ? "No activity has been captured today yet."
                : $"Here are your {results.Count} most recent captured activities from today.";
        }
        else if (TryParseTimeQuery(query, out var targetTime))
        {
            var target = DateTime.Today + targetTime;
            results = ordered
                .Where(x => x.CreatedAt.Date == DateTime.Today)
                .OrderBy(x => Math.Abs((x.CreatedAt - target).TotalMinutes))
                .Take(8)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            answer = results.Count == 0
                ? $"I couldn't find captured activity around {target:h:mm tt}."
                : $"These are the activities closest to {target:h:mm tt}.";
        }
        else
        {
            var terms = Regex.Matches(lowered, @"[a-z0-9][a-z0-9._-]+")
                .Select(m => m.Value)
                .Where(x => x.Length >= 3)
                .Where(x => !AskStopWords.Contains(x))
                .Distinct()
                .ToList();

            results = ordered
                .Where(item => terms.Count == 0 || terms.Any(term => item.SearchableText.Contains(term)))
                .Take(30)
                .ToList();

            answer = results.Count == 0
                ? "I couldn't find anything in your captured timeline that matches that question."
                : $"I found {results.Count} relevant activit{(results.Count == 1 ? "y" : "ies")} in your local timeline.";
        }

        AskAnswerText.Text = answer;
        AskResultsList.ItemsSource = results;
        AskEmptyState.Visibility = Visibility.Collapsed;
        AskResultsState.Visibility = Visibility.Visible;
    }

    private static bool TryParseTimeQuery(string query, out TimeSpan time)
    {
        time = default;
        var match = Regex.Match(query, @"(?<!\d)(\d{1,2})(?::(\d{2}))?\s*(am|pm)?", RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var hour))
            return false;

        var minute = 0;
        if (match.Groups[2].Success && !int.TryParse(match.Groups[2].Value, out minute))
            return false;

        var suffix = match.Groups[3].Value.ToLowerInvariant();
        if (suffix == "pm" && hour < 12) hour += 12;
        if (suffix == "am" && hour == 12) hour = 0;

        if (hour is < 0 or > 23 || minute is < 0 or > 59)
            return false;

        time = new TimeSpan(hour, minute, 0);
        return true;
    }

    private static readonly HashSet<string> AskStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "what", "was", "were", "doing", "around", "show", "find", "from", "that",
        "this", "with", "when", "where", "which", "before", "after", "opening",
        "today", "yesterday", "activity", "worked", "viewed", "memory", "trail",
        "earlier", "recent", "recently", "week", "most"
    };

    private void StoreItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _view.Refresh();
        UpdateLiveActivity();
        UpdateTodaySummary();

        if (TimelineList.SelectedItem is null && _view.Cast<object>().FirstOrDefault() is MemoryItem latest)
            TimelineList.SelectedItem = latest;
    }

    private void UpdateLiveActivity()
    {
        foreach (var item in App.Store.Items)
            item.IsLiveActivity = false;

        var latestVisible = _view.Cast<object>()
            .OfType<MemoryItem>()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (latestVisible is not null)
            latestVisible.IsLiveActivity = true;
    }

    private void UpdateTodaySummary()
    {
        var today = App.Store.Items
            .Where(x => x.CreatedAt.Date == DateTime.Today)
            .ToList();

        TotalActivitiesText.Text = today.Count.ToString();
        AppsUsedText.Text = today
            .Select(x => x.SourceApp)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ToString();

        ClipboardItemsText.Text = today.Count(x => x.SourceKind == SourceKind.Clipboard).ToString();

        if (today.Count > 1)
        {
            var first = today.Min(x => x.CreatedAt);
            var last = today.Max(x => x.CreatedAt);
            var span = last - first;
            ActiveTimeText.Text = span.TotalHours >= 1
                ? $"{(int)span.TotalHours}h {span.Minutes}m"
                : $"{Math.Max(0, span.Minutes)}m";
        }
        else
        {
            ActiveTimeText.Text = "0m";
        }

        var topApps = today
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceApp))
            .GroupBy(x => x.SourceApp, StringComparer.OrdinalIgnoreCase)
            .Select(g => new
            {
                Name = g.Key,
                Count = g.Count(),
                Process = g.Select(x => x.SourceProcess).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? ""
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(5)
            .ToList();

        var max = Math.Max(1, topApps.FirstOrDefault()?.Count ?? 1);

        var names = new[] { TopApp1Name, TopApp2Name, TopApp3Name, TopApp4Name, TopApp5Name };
        var counts = new[] { TopApp1Count, TopApp2Count, TopApp3Count, TopApp4Count, TopApp5Count };
        var bars = new[] { TopApp1Bar, TopApp2Bar, TopApp3Bar, TopApp4Bar, TopApp5Bar };
        var icons = new[] { TopApp1Icon, TopApp2Icon, TopApp3Icon, TopApp4Icon, TopApp5Icon };

        for (var i = 0; i < names.Length; i++)
        {
            if (i < topApps.Count)
            {
                var app = topApps[i];
                names[i].Text = app.Name;
                counts[i].Text = app.Count.ToString();
                bars[i].Maximum = max;
                bars[i].Value = app.Count;
                icons[i].Source = AppIconConverter.ResolveAppIcon(app.Name, app.Process);
                names[i].Opacity = counts[i].Opacity = bars[i].Opacity = icons[i].Opacity = 1;
            }
            else
            {
                names[i].Text = "—";
                counts[i].Text = "";
                bars[i].Maximum = 1;
                bars[i].Value = 0;
                icons[i].Source = null;
                names[i].Opacity = counts[i].Opacity = bars[i].Opacity = icons[i].Opacity = 0.25;
            }
        }
    }

    private void AddMemory_Click(object sender, RoutedEventArgs e)
    {
        var window = new AddMemoryWindow { Owner = this };
        if (window.ShowDialog() == true && window.CreatedItem is not null)
        {
            App.Store.Add(window.CreatedItem);
            _view.Refresh();
            UpdateLiveActivity();
            TimelineList.SelectedItem = window.CreatedItem;
        }
    }

    private void TimelineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Detail pane is data-bound directly to TimelineList.SelectedItem.
    }

    private void RightSidebarToggle_Click(object sender, RoutedEventArgs e)
    {
        _rightSidebarVisible = !_rightSidebarVisible;
        ApplyRightSidebarVisibility();
    }

    private void ApplyRightSidebarVisibility()
    {
        RightSidebar.Visibility = _rightSidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        DetailsSpacerColumn.Width = _rightSidebarVisible ? new GridLength(16) : new GridLength(0);
        DetailsColumn.Width = _rightSidebarVisible ? new GridLength(0.8, GridUnitType.Star) : new GridLength(0);

        RightSidebarToggleIcon.Text = _rightSidebarVisible ? "\uE76B" : "\uE76C";
        RightSidebarToggleButton.ToolTip = _rightSidebarVisible ? "Hide right sidebar" : "Show right sidebar";
    }

    private void HideDetailsPane_Click(object sender, RoutedEventArgs e)
    {
        _detailsPaneVisible = false;
        DetailsPanel.Visibility = Visibility.Collapsed;
        DetailsGapRow.Height = new GridLength(0);
        DetailsRow.Height = new GridLength(0);
        ShowDetailsButton.Visibility = Visibility.Visible;
    }

    private void ShowDetailsPane_Click(object sender, RoutedEventArgs e) => ShowDetailsPane();

    private void TimelineList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TimelineList.SelectedItem is not MemoryItem)
            return;

        ShowDetailsPane();
        e.Handled = true;
    }

    private void ShowDetailsPane()
    {
        if (!_rightSidebarVisible)
        {
            _rightSidebarVisible = true;
            ApplyRightSidebarVisibility();
        }

        _detailsPaneVisible = true;
        DetailsPanel.Visibility = Visibility.Visible;
        DetailsGapRow.Height = new GridLength(12);
        DetailsRow.Height = new GridLength(1, GridUnitType.Star);
        ShowDetailsButton.Visibility = Visibility.Collapsed;
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MemoryItem item })
        {
            App.Store.ToggleFavorite(item);
            _view.Refresh();
            UpdateLiveActivity();
        }
    }

    private void FavoriteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (TimelineList.SelectedItem is not MemoryItem item)
            return;

        App.Store.ToggleFavorite(item);
        _view.Refresh();
        UpdateLiveActivity();
    }

    private void OpenSelected_Click(object sender, RoutedEventArgs e)
    {
        if (TimelineList.SelectedItem is not MemoryItem item)
            return;

        var target = !string.IsNullOrWhiteSpace(item.SourceUrl)
            ? item.SourceUrl
            : item.SourceExecutable;

        if (string.IsNullOrWhiteSpace(target))
        {
            MessageBox.Show("No reopenable source was captured for this activity.", "Memory Trail",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show("Windows could not open this source.", "Memory Trail",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CopySelectedTitle_Click(object sender, RoutedEventArgs e)
    {
        if (TimelineList.SelectedItem is not MemoryItem item || string.IsNullOrWhiteSpace(item.Title))
            return;

        Clipboard.SetText(item.Title);
    }

    private void ClearTimeline_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"This permanently deletes all {App.Store.Items.Count} timeline items. This action cannot be undone.",
            "Clear Timeline?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
            App.Store.Clear();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e) => ToggleCapture();

    private void ToggleCapture()
    {
        App.Preferences.CapturePaused = !App.Preferences.CapturePaused;
        PauseCaptureCheckBox.IsChecked = App.Preferences.CapturePaused;
        App.Preferences.Save();
        UpdateCaptureState();
    }

    private void UpdateCaptureState()
    {
        PauseButton.Content = App.Preferences.CapturePaused ? "Resume Capture" : "Pause Capture";
        StatusText.Text = App.Preferences.CapturePaused ? "Capture paused" : "Capture active";
        StatusDot.Fill = App.Preferences.CapturePaused
            ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
            : new SolidColorBrush(Color.FromRgb(34, 197, 94));
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingSettings) return;
        App.Preferences.CapturePaused = PauseCaptureCheckBox.IsChecked == true;
        App.Preferences.CaptureAppActivity = AppCaptureCheckBox.IsChecked == true;
        App.Preferences.CaptureClipboard = ClipboardCaptureCheckBox.IsChecked == true;
        App.Preferences.Save();
        UpdateCaptureState();
    }

    private void ApplyExclusions_Click(object sender, RoutedEventArgs e)
    {
        var values = ExclusionsBox.Text
            .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        App.Preferences.ExcludedProcesses = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
        App.Preferences.Save();
        MessageBox.Show("Exclusions updated.", "Memory Trail");
    }

    private void StartWithWindowsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingSettings) return;
        App.Preferences.SetStartWithWindows(StartWithWindowsCheckBox.IsChecked == true);
        App.Preferences.Save();
    }

    private void ShowTutorial_Click(object sender, RoutedEventArgs e) => ShowOnboarding();

    private void ShowOnboarding()
    {
        var welcome = new OnboardingWindow { Owner = this };
        if (welcome.ShowDialog() == true)
        {
            App.Preferences.HasCompletedOnboarding = true;
            App.Preferences.Save();
        }
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        App.Preferences.ResetToDefaults();
        App.Capture.ClearRuntimeCache();
        LoadSettingsIntoUi();
        UpdateCaptureState();
        MessageBox.Show("App cache and preferences were reset. Timeline history was not deleted.", "Memory Trail");
    }

    protected override void OnClosed(EventArgs e)
    {
        App.Store.Items.CollectionChanged -= StoreItems_CollectionChanged;

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnClosed(e);
    }
}
