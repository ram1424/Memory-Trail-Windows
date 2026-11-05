using System.ComponentModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace MemoryLane.Windows;

public partial class MainWindow : Window
{
    // Tutorial revision is intentionally independent from the app patch version.
    // This prevents the six-page tutorial from replaying on every 1.0.x update.
    private const string TutorialRevision = "official-v1";

    private readonly ICollectionView _view;
    private Forms.NotifyIcon? _trayIcon;
    private MemoryItem? _lastLiveItem;
    private bool _quitting;
    private string _mode = "Timeline";
    private string _category = "All";
    private bool _initializingSettings = true;
    private bool _detailsPaneVisible = true;
    private bool _rightSidebarVisible = true;
    private int _dayOffset;
    private readonly DispatcherTimer _liveTimer;

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
        UpdateCountsAndChrome();
        ApplyRightSidebarVisibility();

        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _liveTimer.Tick += (_, _) =>
        {
            if (!App.Capture.IsRunning)
                App.Capture.Start();

            UpdateLiveActivity();
            UpdateActivityCount();
            UpdateCaptureState();
        };
        _liveTimer.Start();

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
            if (!AppVersion.IsMicrosoftStoreBuild)
                menu.Items.Add("Check for Updates...", null, (_, _) => Dispatcher.BeginInvoke(new Action(CheckForUpdatesFromTray)));

            menu.Items.Add("What's New", null, (_, _) => Dispatcher.BeginInvoke(new Action(() => ShowWhatsNewWindow(false))));
            menu.Items.Add(new Forms.ToolStripSeparator());
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

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;

        var tutorialIsCurrent =
            App.Preferences.HasCompletedOnboarding &&
            string.Equals(
                App.Preferences.LastCompletedTutorialVersion,
                TutorialRevision,
                StringComparison.OrdinalIgnoreCase);

        if (!tutorialIsCurrent)
        {
            try
            {
                // Version 1 is the first official release. Development builds may have
                // already set HasCompletedOnboarding, so use a version marker to ensure
                // the complete six-page tutorial is shown once for the official release.
                if (ShowOnboarding())
                    ShowWhatsNewIfNeeded();
            }
            catch (Exception ex)
            {
                App.LogNonFatal("Onboarding", ex);
                MessageBox.Show(
                    "Memory Trail started successfully, but the welcome tutorial could not be opened. You can replay it later from Settings.",
                    "Memory Trail",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        else
        {
            ShowWhatsNewIfNeeded();
        }

        await MaybeCheckForUpdatesAsync();
    }

    private void LoadSettingsIntoUi()
    {
        PauseCaptureCheckBox.IsChecked = App.Preferences.CapturePaused;
        AppCaptureCheckBox.IsChecked = App.Preferences.CaptureAppActivity;
        ClipboardCaptureCheckBox.IsChecked = App.Preferences.CaptureClipboard;
        StartWithWindowsCheckBox.IsChecked = App.Preferences.StartWithWindows;
        AutomaticUpdateCheckBox.IsChecked = App.Preferences.AutomaticUpdateChecks;
        AboutVersionText.Text = AppVersion.FriendlyVersion;
        StoragePathText.Text = App.Store.DataFolder;
        StartWithWindowsCheckBox.IsEnabled = true;

        if (AppVersion.IsMicrosoftStoreBuild)
        {
            AutomaticUpdateCheckBox.IsChecked = false;
            AutomaticUpdateCheckBox.IsEnabled = false;
            UpdateStatusText.Text = "Microsoft Store keeps this copy updated — no wrench required.";
        }
        else
        {
            UpdateStatusText.Text = App.Preferences.LastUpdateCheckUtc is DateTimeOffset checkedAt
                ? $"Last checked {checkedAt.ToLocalTime():MMM d, yyyy h:mm tt}."
                : "Updates have not been checked yet.";
        }
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
        var showAsk = mode == "Ask";
        var showInsights = mode == "Insights";
        var showActivity = mode is "Timeline" or "Favorites" or "Clipboard";

        ActivityShell.Visibility = showActivity ? Visibility.Visible : Visibility.Collapsed;
        AskMemoryTrailPanel.Visibility = showAsk ? Visibility.Visible : Visibility.Collapsed;
        InsightsPanel.Visibility = showInsights ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
        PrivacyPanel.Visibility = Visibility.Collapsed;

        _view.Refresh();
        UpdateModeChrome();
        UpdateLiveActivity();
        UpdateCountsAndChrome();

        if (showAsk)
            Dispatcher.BeginInvoke(new Action(() => AskMemoryTrailBox.Focus()));
    }

    private void UpdateNavSelection(string mode)
    {
        var buttons = new[]
        {
            (Button: TimelineNavButton, Mode: "Timeline"),
            (Button: AskMemoryTrailNavButton, Mode: "Ask"),
            (Button: FavoritesNavButton, Mode: "Favorites"),
            (Button: ClipboardNavButton, Mode: "Clipboard"),
            (Button: InsightsNavButton, Mode: "Insights"),
            (Button: SettingsNavButton, Mode: "Settings")
        };

        foreach (var entry in buttons)
        {
            var selected = entry.Mode == mode;
            entry.Button.Background = selected
                ? new LinearGradientBrush(
                    Color.FromRgb(20, 128, 255),
                    Color.FromRgb(51, 110, 242),
                    new Point(0, 0.5),
                    new Point(1, 0.5))
                : Brushes.Transparent;
            entry.Button.Foreground = selected
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(26, 36, 56));
            entry.Button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string category)
            return;

        var modeChanged = _mode != "Timeline";
        if (!modeChanged && string.Equals(_category, category, StringComparison.Ordinal))
            return;

        _category = category;

        if (modeChanged)
        {
            _mode = "Timeline";
            UpdateNavSelection("Timeline");
            ActivityShell.Visibility = Visibility.Visible;
            AskMemoryTrailPanel.Visibility = Visibility.Collapsed;
            InsightsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            PrivacyPanel.Visibility = Visibility.Collapsed;
        }

        // Update the selected tab immediately so the UI feels responsive.
        UpdateCategorySelection(category);

        // Filtering is the only data operation required here. Counts do not change
        // when the user switches tabs, so avoid the full count/summary recomputation.
        _view.Refresh();
        UpdateActivityCount();

        if (modeChanged)
            UpdateModeChrome();

        // Paint the selected filter first, then refresh the live marker at background priority.
        Dispatcher.BeginInvoke(
            new Action(UpdateLiveActivity),
            System.Windows.Threading.DispatcherPriority.Background);
    }

    private void UpdateCategorySelection(string category)
    {
        var sidebarButtons = new[]
        {
            (Button: AllAppsFilter, Category: "All"),
            (Button: BrowserFilter, Category: "Browser"),
            (Button: EmailFilter, Category: "Email"),
            (Button: ChatFilter, Category: "Chat"),
            (Button: FilesFilter, Category: "Files"),
            (Button: OfficeFilter, Category: "Office"),
            (Button: DevelopmentFilter, Category: "Development"),
            (Button: MediaFilter, Category: "Media"),
            (Button: DesignFilter, Category: "Design"),
            (Button: RemoteFilter, Category: "Remote"),
            (Button: SystemFilter, Category: "System"),
            (Button: OtherFilter, Category: "Other")
        };

        foreach (var entry in sidebarButtons)
        {
            var selected = _mode == "Timeline" && entry.Category == category;
            entry.Button.Background = selected
                ? new SolidColorBrush(Color.FromArgb(38, 22, 119, 255))
                : Brushes.Transparent;
            entry.Button.Foreground = new SolidColorBrush(Color.FromRgb(26, 36, 56));
            entry.Button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Medium;
        }

        var quickButtons = new[]
        {
            (Button: QuickAllFilter, Count: QuickAllCountText, Category: "All"),
            (Button: QuickBrowserFilter, Count: QuickBrowserCountText, Category: "Browser"),
            (Button: QuickEmailFilter, Count: QuickEmailCountText, Category: "Email"),
            (Button: QuickChatFilter, Count: QuickChatCountText, Category: "Chat"),
            (Button: QuickFilesFilter, Count: QuickFilesCountText, Category: "Files"),
            (Button: QuickOfficeFilter, Count: QuickOfficeCountText, Category: "Office")
        };

        foreach (var entry in quickButtons)
        {
            var selected = entry.Category == category;
            entry.Button.Background = selected
                ? new SolidColorBrush(Color.FromRgb(22, 119, 255))
                : new SolidColorBrush(Color.FromRgb(246, 249, 252));
            entry.Button.BorderBrush = selected
                ? new SolidColorBrush(Color.FromRgb(22, 128, 255))
                : new SolidColorBrush(Color.FromRgb(216, 225, 236));
            entry.Button.Foreground = selected
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(26, 36, 56));

            // Count text has its own foreground in XAML, so explicitly switch it
            // to white when the quick filter is selected for strong contrast.
            entry.Count.Foreground = selected
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(102, 119, 143));
        }
    }

    private bool FilterItem(object item)
    {
        if (item is not MemoryItem memory) return false;

        if (_mode == "Favorites")
            return memory.IsFavorite;

        if (_mode == "Clipboard")
            return memory.SourceKind == SourceKind.Clipboard;

        if (_mode != "Timeline")
            return true;

        var day = SelectedDay.Date;
        if (memory.CreatedAt.Date != day)
            return false;

        if (_category != "All" && !MatchesCategory(memory, _category))
            return false;

        return true;
    }

    private static bool MatchesCategory(MemoryItem memory, string category)
    {
        var label = memory.CategoryLabel;

        return category switch
        {
            "Browser" => label == "Web",
            "Email" => label == "Email",
            "Chat" => label == "Chat",
            "Files" => label == "Files",
            "Office" => label == "Productivity",
            "Development" => label == "Development",
            "Media" => label == "Media",
            "Design" => label == "Design",
            "Remote" => label == "Remote Access",
            "System" => label == "System",
            "Other" => label == "Other",
            _ => true
        };
    }

    private DateTime SelectedDay => DateTime.Today.AddDays(_dayOffset);

    private void PreviousDay_Click(object sender, RoutedEventArgs e)
    {
        _dayOffset--;
        RefreshTimelineState();
    }

    private void NextDay_Click(object sender, RoutedEventArgs e)
    {
        if (_dayOffset >= 0) return;
        _dayOffset++;
        RefreshTimelineState();
    }

    private void RefreshTimelineState()
    {
        _view.Refresh();
        UpdateModeChrome();
        UpdateLiveActivity();
        UpdateTodaySummary();
        UpdateCountsAndChrome();

        if (_view.Cast<object>().FirstOrDefault() is MemoryItem first)
            TimelineList.SelectedItem = first;
    }

    private void MoreFilters_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var entry in new[]
        {
            ("Development", "Development"),
            ("Media", "Media"),
            ("Design", "Design"),
            ("Remote Access", "Remote"),
            ("System", "System"),
            ("Other", "Other")
        })
        {
            var item = new MenuItem
            {
                Header = entry.Item1,
                Tag = entry.Item2,
                Icon = new CategoryIcon
                {
                    Category = entry.Item2,
                    Width = 22,
                    Height = 22,
                    Margin = new Thickness(0, 0, 3, 0)
                }
            };
            item.Click += (_, _) =>
            {
                _category = (string)item.Tag;
                UpdateCategorySelection(_category);
                _view.Refresh();
                UpdateLiveActivity();
                UpdateActivityCount();
            };
            menu.Items.Add(item);
        }

        menu.PlacementTarget = MoreFiltersButton;
        menu.IsOpen = true;
    }

    private void MoreRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MemoryItem item } button)
            return;

        TimelineList.SelectedItem = item;
        var menu = new ContextMenu();

        var copy = new MenuItem { Header = "Copy Details" };
        copy.Click += (_, _) =>
        {
            var text = string.Join(Environment.NewLine,
                item.Title,
                item.SourceApp,
                item.CreatedAt.ToString("g"),
                item.Body);
            Clipboard.SetText(text);
        };
        menu.Items.Add(copy);

        var open = new MenuItem { Header = "Open Source App" };
        open.Click += (_, _) => OpenSelected_Click(button, new RoutedEventArgs());
        menu.Items.Add(open);

        menu.Items.Add(new Separator());

        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (_, _) =>
        {
            App.Store.Delete(item);
            _view.Refresh();
            UpdateLiveActivity();
            UpdateTodaySummary();
            UpdateCountsAndChrome();
        };
        menu.Items.Add(delete);

        menu.PlacementTarget = button;
        menu.IsOpen = true;
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
        UpdateCountsAndChrome();

        if (TimelineList.SelectedItem is null && _view.Cast<object>().FirstOrDefault() is MemoryItem latest)
            TimelineList.SelectedItem = latest;
    }

    private void UpdateLiveActivity()
    {
        if (_lastLiveItem is not null)
        {
            _lastLiveItem.IsLiveActivity = false;
            _lastLiveItem.IsLatestActivity = false;
            _lastLiveItem = null;
        }

        if (_mode != "Timeline" || _dayOffset != 0)
            return;

        // The collection view is already sorted newest-first, so avoid an extra OrderBy
        // and avoid resetting flags on every stored activity.
        var latestVisible = _view.Cast<object>()
            .OfType<MemoryItem>()
            .FirstOrDefault();

        if (latestVisible is null)
            return;

        latestVisible.IsLatestActivity = true;
        latestVisible.IsLiveActivity =
            (DateTime.Now - latestVisible.CreatedAt).TotalMinutes <= 2;

        _lastLiveItem = latestVisible;
    }

    private void UpdateModeChrome()
    {
        var timelineMode = _mode == "Timeline";
        QuickFiltersBar.Visibility = timelineMode ? Visibility.Visible : Visibility.Collapsed;
        RightSidebarToggleButton.Visibility = timelineMode ? Visibility.Visible : Visibility.Collapsed;

        if (timelineMode)
        {
            PageTitleText.Text = _dayOffset == 0 ? "Today" : SelectedDay.ToString("dddd");
            PageSubtitleText.Text = SelectedDay.ToString("dddd, MMMM d, yyyy");
        }
        else if (_mode == "Favorites")
        {
            PageTitleText.Text = "Favorites";
            PageSubtitleText.Text = "Keep important memories close.";
        }
        else if (_mode == "Clipboard")
        {
            PageTitleText.Text = "Clipboard History";
            PageSubtitleText.Text = "Quickly find text and links you copied.";
        }

        DayLabelText.Text = _dayOffset == 0 ? "Today" : SelectedDay.ToString("MMM d");
        RightDateText.Text = SelectedDay.ToString("ddd, MMM d, yyyy");
        NextDayButton.IsEnabled = _dayOffset < 0;
        ApplyRightSidebarVisibility();
        UpdateActivityCount();
    }

    private void UpdateActivityCount()
    {
        var count = _view.Cast<object>().OfType<MemoryItem>().Count();
        ActivityCountText.Text = $"{count} activit{(count == 1 ? "y" : "ies")}";
    }

    private void UpdateCountsAndChrome()
    {
        var all = App.Store.Items.ToList();

        SetFilterCount(AllAppsCountText, AllAppsFilter, all.Count, alwaysVisible: true);
        SetFilterCount(BrowserCountText, BrowserFilter, all.Count(x => MatchesCategory(x, "Browser")));
        SetFilterCount(EmailCountText, EmailFilter, all.Count(x => MatchesCategory(x, "Email")));
        SetFilterCount(ChatCountText, ChatFilter, all.Count(x => MatchesCategory(x, "Chat")));
        SetFilterCount(FilesCountText, FilesFilter, all.Count(x => MatchesCategory(x, "Files")));
        SetFilterCount(OfficeCountText, OfficeFilter, all.Count(x => MatchesCategory(x, "Office")));
        SetFilterCount(DevelopmentCountText, DevelopmentFilter, all.Count(x => MatchesCategory(x, "Development")));
        SetFilterCount(MediaCountText, MediaFilter, all.Count(x => MatchesCategory(x, "Media")));
        SetFilterCount(DesignCountText, DesignFilter, all.Count(x => MatchesCategory(x, "Design")));
        SetFilterCount(RemoteCountText, RemoteFilter, all.Count(x => MatchesCategory(x, "Remote")));
        SetFilterCount(SystemCountText, SystemFilter, all.Count(x => MatchesCategory(x, "System")));
        SetFilterCount(OtherCountText, OtherFilter, all.Count(x => MatchesCategory(x, "Other")));

        var dayItems = all.Where(x => x.CreatedAt.Date == SelectedDay.Date).ToList();
        var browser = dayItems.Count(x => MatchesCategory(x, "Browser"));
        var email = dayItems.Count(x => MatchesCategory(x, "Email"));
        var chat = dayItems.Count(x => MatchesCategory(x, "Chat"));
        var files = dayItems.Count(x => MatchesCategory(x, "Files"));
        var office = dayItems.Count(x => MatchesCategory(x, "Office"));

        QuickAllCountText.Text = $" ({dayItems.Count})";
        QuickBrowserCountText.Text = $" ({browser})";
        QuickEmailCountText.Text = $" ({email})";
        QuickChatCountText.Text = $" ({chat})";
        QuickFilesCountText.Text = $" ({files})";
        QuickOfficeCountText.Text = $" ({office})";

        QuickAllFilter.Visibility = Visibility.Visible;
        QuickBrowserFilter.Visibility = browser > 0 ? Visibility.Visible : Visibility.Collapsed;
        QuickEmailFilter.Visibility = email > 0 ? Visibility.Visible : Visibility.Collapsed;
        QuickChatFilter.Visibility = chat > 0 ? Visibility.Visible : Visibility.Collapsed;
        QuickFilesFilter.Visibility = files > 0 ? Visibility.Visible : Visibility.Collapsed;
        QuickOfficeFilter.Visibility = office > 0 ? Visibility.Visible : Visibility.Collapsed;

        var hasOverflow = dayItems.Any(x =>
            MatchesCategory(x, "Development") ||
            MatchesCategory(x, "Media") ||
            MatchesCategory(x, "Design") ||
            MatchesCategory(x, "Remote") ||
            MatchesCategory(x, "System") ||
            MatchesCategory(x, "Other"));
        MoreFiltersButton.Visibility = hasOverflow ? Visibility.Visible : Visibility.Collapsed;

        UpdateActivityCount();
        UpdateModeChrome();
    }

    private static void SetFilterCount(TextBlock block, Button button, int value, bool alwaysVisible = false)
    {
        block.Text = value.ToString();
        button.Visibility = alwaysVisible || value > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateTodaySummary()
    {
        var today = App.Store.Items
            .Where(x => x.CreatedAt.Date == SelectedDay.Date)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        TotalActivitiesText.Text = today.Count.ToString();
        AppsUsedText.Text = today
            .Select(x => x.SourceApp)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ToString();

        ClipboardItemsText.Text = today.Count(x => x.SourceKind == SourceKind.Clipboard).ToString();
        FilesTodayText.Text = today.Count(x => MatchesCategory(x, "Files")).ToString();

        var activeTime = CalculateActiveTime(today);
        ActiveTimeText.Text = FormatDuration(activeTime);

        UpdateTodayDashboard(today);
        UpdateInsights();
    }

    private void UpdateTodayDashboard(IReadOnlyList<MemoryItem> today)
    {
        TodayTopAppsPanel.Children.Clear();
        var topApps = today
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceApp))
            .GroupBy(x => x.SourceApp, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Name = g.Key.Trim(), Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(6)
            .ToList();

        foreach (var app in topApps)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            row.Children.Add(new TextBlock
            {
                Text = app.Name,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(20, 29, 43)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var count = new TextBlock
            {
                Text = app.Count + (app.Count == 1 ? " activity" : " activities"),
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.FromRgb(96, 112, 135))
            };
            Grid.SetColumn(count, 1);
            row.Children.Add(count);
            TodayTopAppsPanel.Children.Add(row);
        }

        if (topApps.Count == 0)
            TodayTopAppsPanel.Children.Add(new TextBlock { Text = "No app activity yet.", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(96, 112, 135)) });

        var hourly = Enumerable.Range(0, 24).Select(h => today.Count(x => x.CreatedAt.Hour == h)).ToArray();
        var max = hourly.Length == 0 ? 0 : hourly.Max();
        var peak = max == 0 ? -1 : Array.IndexOf(hourly, max);
        TodayPeakFocusText.Text = peak < 0
            ? "No focus window yet"
            : DateTime.Today.AddHours(peak).ToString("h:mm tt") + " – " + DateTime.Today.AddHours((peak + 1) % 24).ToString("h:mm tt");

        var topType = today
            .GroupBy(x => x.CategoryLabel)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
        TodayTopWorkTypeText.Text = string.IsNullOrWhiteSpace(topType) ? "No work type yet" : topType;
        TodaySwitchingText.Text = topApps.Count == 0 ? "No apps used in this period." : $"You moved across {topApps.Count} app{(topApps.Count == 1 ? "" : "s")} in this period.";

        TodayRecentFilesPanel.Children.Clear();
        var recentFiles = today
            .Where(x => MatchesCategory(x, "Files"))
            .OrderByDescending(x => x.CreatedAt)
            .Take(4)
            .ToList();

        foreach (var item in recentFiles)
        {
            var row = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            row.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(20, 29, 43)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            row.Children.Add(new TextBlock
            {
                Text = item.CreatedAt.ToString("h:mm tt"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(96, 112, 135))
            });
            TodayRecentFilesPanel.Children.Add(row);
        }

        if (recentFiles.Count == 0)
            TodayRecentFilesPanel.Children.Add(new TextBlock { Text = "No recent files or documents.", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(96, 112, 135)) });
    }

    private void UpdateInsights()
    {
        var items = App.Store.Items
            .Where(x => x.CreatedAt.Date == DateTime.Today)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        InsightsDateText.Text = DateTime.Today.ToString("dddd, MMMM d");
        InsightsTotalActivitiesText.Text = items.Count.ToString();
        InsightsAppsUsedText.Text = items
            .Select(x => x.SourceApp)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ToString();
        InsightsFavoritesText.Text = items.Count(x => MatchesCategory(x, "Files")).ToString();

        var activeTime = CalculateActiveTime(items);
        var longestSession = CalculateLongestSession(items);
        InsightsActiveTimeText.Text = FormatDuration(activeTime);
        InsightsFocusTimeText.Text = FormatDuration(activeTime);
        InsightsFocusStreakText.Text = FormatDuration(longestSession);
        InsightsLongestSessionText.Text = FormatDuration(longestSession);

        var hourly = Enumerable.Range(0, 24)
            .Select(hour => items.Count(x => x.CreatedAt.Hour == hour))
            .ToArray();

        var maxHourValue = hourly.Length == 0 ? 0 : hourly.Max();
        var peakHour = maxHourValue == 0 ? -1 : Array.IndexOf(hourly, maxHourValue);
        InsightsPeakTimeText.Text = peakHour < 0
            ? "—"
            : DateTime.Today.AddHours(peakHour).ToString("h tt") + "–" +
              DateTime.Today.AddHours((peakHour + 1) % 24).ToString("h tt");

        InsightsActivityChartPanel.Children.Clear();
        for (var hour = 0; hour < 24; hour++)
        {
            var cell = new Grid { Margin = new Thickness(2, 0, 2, 0) };
            var height = maxHourValue == 0
                ? 4
                : Math.Max(4, 92d * hourly[hour] / maxHourValue);

            cell.Children.Add(new Border
            {
                Height = height,
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(
                    hour == peakHour
                        ? Color.FromRgb(22, 119, 255)
                        : Color.FromRgb(185, 198, 216))
            });

            InsightsActivityChartPanel.Children.Add(cell);
        }

        var topApps = items
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceApp))
            .GroupBy(x => x.SourceApp, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Name = g.Key.Trim(), Count = g.Count(), Sample = g.First() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(4)
            .ToList();

        InsightsTopItemsPanel.Children.Clear();
        if (topApps.Count == 0)
        {
            InsightsTopItemsPanel.Children.Add(new TextBlock
            {
                Text = "No top items yet — your trail is still warming up.",
                Foreground = new SolidColorBrush(Color.FromRgb(82, 99, 122)),
                FontSize = 11
            });
        }
        else
        {
            var accents = new[]
            {
                Color.FromRgb(80, 132, 242),
                Color.FromRgb(47, 173, 145),
                Color.FromRgb(128, 91, 206),
                Color.FromRgb(232, 139, 79)
            };

            for (var index = 0; index < topApps.Count; index++)
            {
                var top = topApps[index];
                var row = new Grid { Margin = new Thickness(0, 4, 0, 5) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var icon = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(Color.FromArgb(32, accents[index].R, accents[index].G, accents[index].B)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                icon.Child = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(top.Name) ? "?" : top.Name[..1].ToUpperInvariant(),
                    Foreground = new SolidColorBrush(accents[index]),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                row.Children.Add(icon);

                var labels = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                labels.Children.Add(new TextBlock
                {
                    Text = top.Name,
                    Foreground = new SolidColorBrush(Color.FromRgb(18, 26, 43)),
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                labels.Children.Add(new TextBlock
                {
                    Text = top.Sample.CategoryLabel,
                    Foreground = new SolidColorBrush(Color.FromRgb(112, 128, 150)),
                    FontSize = 9.5
                });
                Grid.SetColumn(labels, 1);
                row.Children.Add(labels);

                var count = new TextBlock
                {
                    Text = top.Count.ToString() + " activities",
                    Foreground = new SolidColorBrush(Color.FromRgb(82, 99, 122)),
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(count, 2);
                row.Children.Add(count);

                InsightsTopItemsPanel.Children.Add(row);
            }
        }

        var categories = items
            .GroupBy(x => x.CategoryLabel)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        InsightsDistributionPanel.Children.Clear();
        var categoryColors = new[]
        {
            Color.FromRgb(71, 126, 237),
            Color.FromRgb(43, 166, 139),
            Color.FromRgb(135, 97, 204),
            Color.FromRgb(230, 139, 80),
            Color.FromRgb(111, 127, 150)
        };

        for (var index = 0; index < categories.Count; index++)
        {
            var category = categories[index];
            var percent = items.Count == 0 ? 0 : 100d * category.Count / items.Count;
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 9) };

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            header.Children.Add(new TextBlock
            {
                Text = category.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(49, 68, 95)),
                FontSize = 10.5
            });

            var percentText = new TextBlock
            {
                Text = Math.Round(percent).ToString("0") + "%",
                Foreground = new SolidColorBrush(Color.FromRgb(82, 99, 122)),
                FontSize = 9.5
            };
            Grid.SetColumn(percentText, 1);
            header.Children.Add(percentText);
            panel.Children.Add(header);

            panel.Children.Add(new ProgressBar
            {
                Height = 7,
                Maximum = 100,
                Value = percent,
                Foreground = new SolidColorBrush(categoryColors[index]),
                Background = new SolidColorBrush(Color.FromRgb(232, 237, 244)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 4, 0, 0)
            });

            InsightsDistributionPanel.Children.Add(panel);
        }

        var mostActive = topApps.FirstOrDefault();
        InsightsMostActiveAppText.Text = mostActive?.Name ?? "—";
        InsightsMostActiveAppSubtext.Text = mostActive is null
            ? "No activity yet"
            : mostActive.Count.ToString() + " activities today";

        var topSite = items
            .Where(x => !string.IsNullOrWhiteSpace(x.SourceUrl))
            .Select(x => TryGetHost(x.SourceUrl!))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Host = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        InsightsTopWebsiteText.Text = topSite?.Host ?? "—";
        InsightsTopWebsiteSubtext.Text = topSite is null
            ? "No web activity yet"
            : topSite.Count.ToString() + " visits/activity entries";

        InsightsDistributionSummaryText.Text = categories.Count == 0
            ? "No distribution yet — give the day a little more runway."
            : string.Join("  •  ", categories.Take(3).Select(x =>
            {
                var pct = items.Count == 0 ? 0 : (int)Math.Round(100d * x.Count / items.Count);
                return x.Name + " " + pct + "%";
            }));

        InsightsInsightText.Text = peakHour < 0
            ? "Your activity story will get smarter as the day fills in."
            : "Your busiest stretch is around " + DateTime.Today.AddHours(peakHour).ToString("h tt") +
              ". Protect that window when you need uninterrupted focus.";
    }

    private static TimeSpan CalculateActiveTime(IReadOnlyList<MemoryItem> items)
    {
        if (items.Count < 2)
            return TimeSpan.Zero;

        var total = TimeSpan.Zero;
        for (var i = 0; i < items.Count - 1; i++)
        {
            var gap = items[i + 1].CreatedAt - items[i].CreatedAt;
            if (gap > TimeSpan.Zero && gap <= TimeSpan.FromMinutes(5))
                total += gap;
        }

        return total;
    }

    private static TimeSpan CalculateLongestSession(IReadOnlyList<MemoryItem> items)
    {
        if (items.Count < 2)
            return TimeSpan.Zero;

        var longest = TimeSpan.Zero;
        var current = TimeSpan.Zero;

        for (var i = 0; i < items.Count - 1; i++)
        {
            var gap = items[i + 1].CreatedAt - items[i].CreatedAt;
            if (gap > TimeSpan.Zero && gap <= TimeSpan.FromMinutes(5))
            {
                current += gap;
                if (current > longest)
                    longest = current;
            }
            else
            {
                current = TimeSpan.Zero;
            }
        }

        return longest;
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return "0m";
        if (duration.TotalMinutes < 1)
            return "<1m";
        if (duration.TotalHours >= 1)
            return ((int)duration.TotalHours).ToString() + "h " + duration.Minutes.ToString() + "m";

        return Math.Max(1, (int)Math.Round(duration.TotalMinutes)).ToString() + "m";
    }

    private static string TryGetHost(string value)
    {
        try
        {
            var uri = new Uri(value);
            return uri.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return "";
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

    private void TimelineList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TimelineList.SelectedItem is not MemoryItem)
            return;

        ShowDetailsPane();
    }

    private void RightSidebarToggle_Click(object sender, RoutedEventArgs e)
    {
        _rightSidebarVisible = !_rightSidebarVisible;
        ApplyRightSidebarVisibility();
    }

    private void ApplyRightSidebarVisibility()
    {
        var show = _mode == "Timeline" && _rightSidebarVisible;
        RightSidebar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        RightDivider.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        DetailsSpacerColumn.Width = show ? new GridLength(1) : new GridLength(0);
        DetailsColumn.Width = show ? new GridLength(360) : new GridLength(0);

        PanelToggleText.Text = show ? "Hide Panels" : "Show Panels";
        RightSidebarToggleButton.ToolTip = show
            ? "Hide Today Summary and Activity Details"
            : "Show Today Summary and Activity Details";
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

    private void ShowDetailsPane()
    {
        if (_mode == "Timeline" && !_rightSidebarVisible)
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
        => ShowHistoryClearDialog(HistoryClearRange.All);

    private void ClearLastWeek_Click(object sender, RoutedEventArgs e)
        => ShowHistoryClearDialog(HistoryClearRange.LastWeek);

    private void ClearLastMonth_Click(object sender, RoutedEventArgs e)
        => ShowHistoryClearDialog(HistoryClearRange.LastMonth);

    private void ClearAllHistory_Click(object sender, RoutedEventArgs e)
        => ShowHistoryClearDialog(HistoryClearRange.All);

    private void ShowHistoryClearDialog(HistoryClearRange range)
    {
        var dialog = new HistoryClearWindow(range) { Owner = this };
        dialog.ShowDialog();

        if (!dialog.HistoryChanged)
            return;

        _view.Refresh();
        UpdateLiveActivity();
        UpdateTodaySummary();
        UpdateCountsAndChrome();

        if (TimelineList.SelectedItem is null && _view.Cast<object>().FirstOrDefault() is MemoryItem latest)
            TimelineList.SelectedItem = latest;
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(App.Store.DataFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = App.Store.DataFolder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Open data folder", ex);
            MessageBox.Show(
                "Windows could not open the Memory Trail data folder.",
                "Memory Trail",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void ExportActivity_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Memory Trail Activity",
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = "Memory-Trail-Activity-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            App.Store.ExportJson(dialog.FileName);
            MessageBox.Show(
                "Trail packed. Your activity export is ready to travel — unlike the original, which is still staying local.",
                "Activity Exported",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Export activity", ex);
            MessageBox.Show(
                "Memory Trail could not export the activity file.",
                "Export Activity",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
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
        var paused = App.Preferences.CapturePaused;
        var appCaptureOff = !App.Preferences.CaptureAppActivity;
        var running = App.Capture.IsRunning;

        var label = paused
            ? "Capture paused"
            : appCaptureOff
                ? "App capture off"
                : running
                    ? "Capturing"
                    : "Starting capture";

        var subtext = paused
            ? "Click to resume"
            : appCaptureOff
                ? "Enable App Activity in Settings"
                : "Your activity is stored locally";

        StatusText.Text = label;
        CaptureSubtext.Text = subtext;
        QuickCaptureText.Text = paused ? "Paused" : appCaptureOff ? "Off" : running ? "Capturing" : "Starting";

        var color = paused
            ? Color.FromRgb(245, 158, 11)
            : appCaptureOff
                ? Color.FromRgb(148, 163, 184)
                : running
                    ? Color.FromRgb(34, 197, 94)
                    : Color.FromRgb(59, 130, 246);

        StatusDot.Fill = new SolidColorBrush(color);
        QuickCaptureDot.Fill = new SolidColorBrush(color);
        RightStatusDot.Fill = new SolidColorBrush(color);
        RightStatusText.Text = paused ? "Paused" : appCaptureOff ? "App capture off" : running ? "Capturing" : "Starting";
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingSettings) return;
        App.Preferences.CapturePaused = PauseCaptureCheckBox.IsChecked == true;
        App.Preferences.CaptureAppActivity = AppCaptureCheckBox.IsChecked == true;
        App.Preferences.CaptureClipboard = ClipboardCaptureCheckBox.IsChecked == true;
        App.Preferences.Save();

        if (App.Preferences.CaptureAppActivity && !App.Preferences.CapturePaused)
        {
            App.Capture.ClearRuntimeCache();
            App.Capture.Start();
        }

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

        var requested = StartWithWindowsCheckBox.IsChecked == true;
        if (!App.Preferences.SetStartWithWindows(requested))
        {
            _initializingSettings = true;
            StartWithWindowsCheckBox.IsChecked = App.Preferences.StartWithWindows;
            _initializingSettings = false;

            MessageBox.Show(
                "Windows would not let Memory Trail add itself to startup. The checkbox has been returned to its previous state.",
                "Start with Windows",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        App.Preferences.Save();
    }

    private void OpenPrivacyPolicy_Click(object sender, RoutedEventArgs e)
    {
        var policy = new PrivacyPolicyWindow { Owner = this };
        policy.ShowDialog();
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/ram1424/Memory-Trail-Windows",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            App.LogNonFatal("OpenGitHub", ex);
            MessageBox.Show(
                "Windows could not open the Memory Trail GitHub page.",
                "Memory Trail",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        => await CheckForUpdatesAsync(manual: true);

    private async void CheckForUpdatesFromTray()
        => await CheckForUpdatesAsync(manual: true);

    private void ShowWhatsNew_Click(object sender, RoutedEventArgs e)
        => ShowWhatsNewWindow(markSeen: true);

    private void AutomaticUpdatesChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingSettings || AppVersion.IsMicrosoftStoreBuild) return;

        App.Preferences.AutomaticUpdateChecks = AutomaticUpdateCheckBox.IsChecked == true;
        App.Preferences.Save();

        if (App.Preferences.AutomaticUpdateChecks)
            _ = CheckForUpdatesAsync(manual: false);
    }

    private async Task MaybeCheckForUpdatesAsync()
    {
        if (AppVersion.IsMicrosoftStoreBuild || !App.Preferences.AutomaticUpdateChecks)
            return;

        await CheckForUpdatesAsync(manual: false);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (AppVersion.IsMicrosoftStoreBuild)
        {
            UpdateStatusText.Text = "Microsoft Store is on update duty — it has the clipboard and everything.";

            if (manual)
            {
                var storeStatus = new UpToDateWindow(storeManaged: true) { Owner = this };
                storeStatus.ShowDialog();
            }

            return;
        }

        try
        {
            UpdateStatusText.Text = "Checking GitHub for updates...";

            var result = await UpdateService.CheckAsync();
            App.Preferences.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
            App.Preferences.Save();

            if (!result.CheckSucceeded)
            {
                UpdateStatusText.Text = result.Message;

                if (manual)
                {
                    var open = MessageBox.Show(
                        result.Message + "\n\nOpen the Memory Trail Releases page in your browser?",
                        "Memory Trail Updates",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (open == MessageBoxResult.Yes)
                        UpdateService.OpenReleasesPage();
                }

                return;
            }

            if (!result.IsUpdateAvailable)
            {
                UpdateStatusText.Text = $"You're up to date — {AppVersion.FriendlyVersion}.";

                if (manual)
                {
                    var upToDate = new UpToDateWindow { Owner = this };
                    upToDate.ShowDialog();
                }

                return;
            }

            if (!manual && ShouldDeferUpdatePrompt(result))
                return;

            if (IsPendingUpdate(result))
            {
                UpdateStatusText.Text =
                    $"{result.ReleaseName} is downloaded and ready to install.";

                if (manual)
                {
                    var restartNow = MessageBox.Show(
                        "This update is already downloaded and ready. Restart Memory Trail now to install it automatically?",
                        "Memory Trail Update Ready",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (restartNow == MessageBoxResult.Yes &&
                        UpdateService.SchedulePendingInstall(
                            App.Preferences,
                            relaunchAfterInstall: true))
                    {
                        await Task.Delay(350);
                        QuitApplication();
                    }
                }

                return;
            }

            UpdateStatusText.Text = $"{result.ReleaseName} is available.";

            var prompt = new UpdateAvailableWindow(result) { Owner = this };
            prompt.ShowDialog();

            var choice = prompt.Choice == UpdatePromptChoice.None
                ? UpdatePromptChoice.RemindLater
                : prompt.Choice;

            switch (choice)
            {
                case UpdatePromptChoice.Update:
                    App.Preferences.UpdateReminderUntilUtc = null;
                    App.Preferences.SkippedUpdateVersion = "";
                    App.Preferences.Save();
                    _ = StartBackgroundUpdateAsync(result);
                    break;

                case UpdatePromptChoice.SkipVersion:
                    App.Preferences.SkippedUpdateVersion = result.ReleaseTag;
                    App.Preferences.UpdateReminderUntilUtc = null;
                    App.Preferences.Save();
                    UpdateStatusText.Text =
                        $"{result.ReleaseName} will be skipped on automatic checks.";
                    break;

                default:
                    App.Preferences.UpdateReminderUntilUtc =
                        DateTimeOffset.UtcNow.AddHours(24);
                    App.Preferences.Save();
                    UpdateStatusText.Text =
                        "Update reminder postponed for 24 hours.";
                    break;
            }
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Update check", ex);
            UpdateStatusText.Text = "Update check could not be completed.";

            if (manual)
            {
                MessageBox.Show(
                    "Memory Trail could not complete the update check. You can still open the Releases page from GitHub.",
                    "Memory Trail Updates",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    private bool ShouldDeferUpdatePrompt(UpdateCheckResult result)
    {
        if (string.Equals(
                App.Preferences.SkippedUpdateVersion,
                result.ReleaseTag,
                StringComparison.OrdinalIgnoreCase))
        {
            UpdateStatusText.Text =
                $"{result.ReleaseName} is available but this version is skipped.";
            return true;
        }

        if (App.Preferences.UpdateReminderUntilUtc is DateTimeOffset reminder &&
            reminder > DateTimeOffset.UtcNow)
        {
            UpdateStatusText.Text =
                $"Update reminder scheduled for {reminder.ToLocalTime():MMM d, h:mm tt}.";
            return true;
        }

        if (App.Preferences.UpdateReminderUntilUtc is not null)
        {
            App.Preferences.UpdateReminderUntilUtc = null;
            App.Preferences.Save();
        }

        return false;
    }

    private static bool IsPendingUpdate(UpdateCheckResult result)
        => !string.IsNullOrWhiteSpace(App.Preferences.PendingUpdateDirectory) &&
           Directory.Exists(App.Preferences.PendingUpdateDirectory) &&
           string.Equals(
               App.Preferences.PendingUpdateVersion,
               result.ReleaseTag,
               StringComparison.OrdinalIgnoreCase) &&
           (
               string.IsNullOrWhiteSpace(result.ReleaseCommitSha) ||
               string.Equals(
                   App.Preferences.PendingUpdateReleaseCommit,
                   result.ReleaseCommitSha,
                   StringComparison.OrdinalIgnoreCase)
           );

    private async Task StartBackgroundUpdateAsync(UpdateCheckResult result)
    {
        try
        {
            UpdateStatusText.Text =
                $"Downloading {result.ReleaseName} in the background...";

            _trayIcon?.ShowBalloonTip(
                4000,
                "Memory Trail Update",
                "Downloading the update in the background. You can keep using Memory Trail.",
                Forms.ToolTipIcon.Info);

            var staged = await UpdateService.StageUpdateAsync(result);

            App.Preferences.PendingUpdateVersion = staged.ReleaseTag;
            App.Preferences.PendingUpdateReleaseCommit = staged.ReleaseCommitSha;
            App.Preferences.PendingUpdateDirectory = staged.PayloadDirectory;
            App.Preferences.Save();

            UpdateStatusText.Text =
                "Update downloaded and verified. Restarting Memory Trail to finish installation...";

            _trayIcon?.ShowBalloonTip(
                4500,
                "Memory Trail Update Ready",
                "The update is ready. Memory Trail will restart automatically to finish installing it.",
                Forms.ToolTipIcon.Info);

            if (!UpdateService.SchedulePendingInstall(
                    App.Preferences,
                    relaunchAfterInstall: true))
            {
                throw new InvalidOperationException(
                    "Memory Trail could not start the silent update installer.");
            }

            // Give the status text/notification a brief chance to render, then perform
            // a real application quit. The updater waits for this process to exit,
            // installs the files, and relaunches Memory Trail automatically.
            await Task.Delay(700);
            QuitApplication();
        }
        catch (Exception ex)
        {
            App.LogNonFatal("Background update", ex);
            UpdateStatusText.Text =
                "The update could not be staged automatically.";

            if (_trayIcon is not null)
            {
                _trayIcon.ShowBalloonTip(
                    7000,
                    "Memory Trail Update",
                    "The silent update could not be prepared. Use Check for Updates in Settings to try again.",
                    Forms.ToolTipIcon.Warning);
            }
            else
            {
                MessageBox.Show(
                    "The update could not be staged automatically. You can use Check for Updates in Settings to try again.",
                    "Memory Trail Update",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    private void ShowWhatsNewIfNeeded()
    {
        if (string.Equals(
                App.Preferences.LastSeenVersion,
                AppVersion.ReleaseTag,
                StringComparison.OrdinalIgnoreCase))
            return;

        ShowWhatsNewWindow(markSeen: true);
    }

    private void ShowWhatsNewWindow(bool markSeen)
    {
        var window = new WhatsNewWindow { Owner = this };
        window.ShowDialog();

        if (markSeen)
        {
            App.Preferences.LastSeenVersion = AppVersion.ReleaseTag;
            App.Preferences.Save();
        }
    }

    private void ShowTutorial_Click(object sender, RoutedEventArgs e) => ShowOnboarding();

    private bool ShowOnboarding()
    {
        var welcome = new OnboardingWindow { Owner = this };
        if (welcome.ShowDialog() != true)
            return false;

        App.Preferences.HasCompletedOnboarding = true;
        App.Preferences.LastCompletedTutorialVersion = TutorialRevision;
        App.Preferences.Save();
        return true;
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        var prompt = new ClearCacheWindow { Owner = this };
        if (prompt.ShowDialog() != true || !prompt.ClearConfirmed)
            return;

        App.Preferences.ResetToDefaults();
        App.Capture.ClearRuntimeCache();
        LoadSettingsIntoUi();
        UpdateCaptureState();

        UpdateStatusText.Text = "App cache cleared. Activity history was preserved.";
    }

    protected override void OnClosed(EventArgs e)
    {
        App.Store.Items.CollectionChanged -= StoreItems_CollectionChanged;
        _liveTimer.Stop();

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnClosed(e);
    }
}
