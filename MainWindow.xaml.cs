using System.ComponentModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
    private readonly ICollectionView _view;
    private Forms.NotifyIcon? _trayIcon;
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
            UpdateLiveActivity();
            UpdateActivityCount();
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

        _category = category;
        if (_mode != "Timeline")
        {
            _mode = "Timeline";
            UpdateNavSelection("Timeline");
            ActivityShell.Visibility = Visibility.Visible;
            AskMemoryTrailPanel.Visibility = Visibility.Collapsed;
            InsightsPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            PrivacyPanel.Visibility = Visibility.Collapsed;
        }

        UpdateCategorySelection(category);
        _view.Refresh();
        UpdateModeChrome();
        UpdateLiveActivity();
        UpdateCountsAndChrome();
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
                ? new SolidColorBrush(Color.FromArgb(28, 22, 128, 255))
                : Brushes.Transparent;
            entry.Button.Foreground = new SolidColorBrush(Color.FromRgb(26, 36, 56));
            entry.Button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Medium;
        }

        var quickButtons = new[]
        {
            (Button: QuickAllFilter, Category: "All"),
            (Button: QuickBrowserFilter, Category: "Browser"),
            (Button: QuickEmailFilter, Category: "Email"),
            (Button: QuickChatFilter, Category: "Chat"),
            (Button: QuickFilesFilter, Category: "Files"),
            (Button: QuickOfficeFilter, Category: "Office")
        };

        foreach (var entry in quickButtons)
        {
            var selected = entry.Category == category;
            entry.Button.Background = selected
                ? new SolidColorBrush(Color.FromRgb(22, 128, 255))
                : new SolidColorBrush(Color.FromRgb(246, 249, 252));
            entry.Button.BorderBrush = selected
                ? new SolidColorBrush(Color.FromRgb(22, 128, 255))
                : new SolidColorBrush(Color.FromRgb(216, 225, 236));
            entry.Button.Foreground = selected ? Brushes.White : new SolidColorBrush(Color.FromRgb(26, 36, 56));
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
            var item = new MenuItem { Header = entry.Item1, Tag = entry.Item2 };
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
        foreach (var item in App.Store.Items)
        {
            item.IsLiveActivity = false;
            item.IsLatestActivity = false;
        }

        if (_mode != "Timeline" || _dayOffset != 0)
            return;

        var latestVisible = _view.Cast<object>()
            .OfType<MemoryItem>()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (latestVisible is null)
            return;

        if ((DateTime.Now - latestVisible.CreatedAt).TotalMinutes <= 2)
            latestVisible.IsLiveActivity = true;
        else
            latestVisible.IsLatestActivity = true;
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
            .ToList();

        TotalActivitiesText.Text = today.Count.ToString();
        AppsUsedText.Text = today
            .Select(x => x.SourceApp)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ToString();

        ClipboardItemsText.Text = today.Count(x => x.SourceKind == SourceKind.Clipboard).ToString();

        InsightsTotalActivitiesText.Text = App.Store.Items.Count(x => x.CreatedAt.Date == DateTime.Today).ToString();
        InsightsAppsUsedText.Text = App.Store.Items
            .Where(x => x.CreatedAt.Date == DateTime.Today)
            .Select(x => x.SourceApp)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()
            .ToString();
        InsightsFavoritesText.Text = App.Store.Items.Count(x => x.CreatedAt.Date == DateTime.Today && x.IsFavorite).ToString();
        InsightsClipboardText.Text = App.Store.Items.Count(x => x.CreatedAt.Date == DateTime.Today && x.SourceKind == SourceKind.Clipboard).ToString();

        var categorySummary = App.Store.Items
            .Where(x => x.CreatedAt.Date == DateTime.Today)
            .GroupBy(x => x.CategoryLabel)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}");
        InsightsCategoryText.Text = string.Join("   •   ", categorySummary);

        if (today.Count > 1)
        {
            var ascending = today.OrderBy(x => x.CreatedAt).ToList();
            var activeSeconds = 0d;
            var inactivityThreshold = TimeSpan.FromMinutes(5);

            for (var i = 0; i < ascending.Count - 1; i++)
            {
                var gap = ascending[i + 1].CreatedAt - ascending[i].CreatedAt;
                if (gap > TimeSpan.Zero && gap <= inactivityThreshold)
                    activeSeconds += gap.TotalSeconds;
            }

            var totalSeconds = Math.Max(0, (int)activeSeconds);
            if (totalSeconds == 0)
                ActiveTimeText.Text = "0m";
            else if (totalSeconds < 60)
                ActiveTimeText.Text = "<1m";
            else
            {
                var hours = totalSeconds / 3600;
                var minutes = (totalSeconds % 3600) / 60;
                ActiveTimeText.Text = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
            }
        }
        else
        {
            ActiveTimeText.Text = "0m";
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
        var show = _mode == "Timeline" && _rightSidebarVisible;
        RightSidebar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        RightDivider.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        DetailsSpacerColumn.Width = show ? new GridLength(1) : new GridLength(0);
        DetailsColumn.Width = show ? new GridLength(365) : new GridLength(0);

        PanelToggleText.Text = show ? "Hide Panels" : "Show Panels";
        RightSidebarToggleIcon.Text = "▥";
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
        StatusText.Text = App.Preferences.CapturePaused ? "Capture paused" : "Capturing";
        CaptureSubtext.Text = App.Preferences.CapturePaused ? "Click to resume" : "Your activity is stored locally";
        QuickCaptureText.Text = App.Preferences.CapturePaused ? "Paused" : "Capturing";

        var color = App.Preferences.CapturePaused
            ? Color.FromRgb(245, 158, 11)
            : Color.FromRgb(34, 197, 94);
        StatusDot.Fill = new SolidColorBrush(color);
        QuickCaptureDot.Fill = new SolidColorBrush(color);
        RightStatusDot.Fill = new SolidColorBrush(color);
        RightStatusText.Text = App.Preferences.CapturePaused ? "Paused" : "Capturing";
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
        _liveTimer.Stop();

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnClosed(e);
    }
}
