using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        TimelineList.ItemsSource = _view;

        LoadSettingsIntoUi();
        UpdateCaptureState();
        UpdateNavSelection("Timeline");
        UpdateCategorySelection("All");

        if (App.Store.Items.Count > 0)
            TimelineList.SelectedIndex = 0;

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
            var executable = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executable))
            {
                using var associated = System.Drawing.Icon.ExtractAssociatedIcon(executable);
                if (associated is not null)
                    return (System.Drawing.Icon)associated.Clone();
            }
        }
        catch { }

        return System.Drawing.SystemIcons.Application;
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

        ActivityShell.Visibility = showSettings || showPrivacy ? Visibility.Collapsed : Visibility.Visible;
        SettingsPanel.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
        PrivacyPanel.Visibility = showPrivacy ? Visibility.Visible : Visibility.Collapsed;

        _view.Refresh();

        if (mode == "Search")
            Dispatcher.BeginInvoke(new Action(() => SearchBox.Focus()));
    }

    private void UpdateNavSelection(string mode)
    {
        var buttons = new[]
        {
            (Button: TimelineNavButton, Mode: "Timeline"),
            (Button: SearchNavButton, Mode: "Search"),
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
                ? new SolidColorBrush(Color.FromRgb(27, 82, 154))
                : Brushes.Transparent;
            entry.Button.Foreground = selected
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(196, 209, 227));
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
                ? Color.FromRgb(22, 139, 255)
                : Color.FromRgb(16, 29, 49));
            entry.Button.BorderBrush = new SolidColorBrush(selected
                ? Color.FromRgb(22, 139, 255)
                : Color.FromRgb(38, 58, 87));
            entry.Button.Foreground = selected ? Brushes.White : new SolidColorBrush(Color.FromRgb(211, 222, 237));
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

        var q = SearchBox.Text?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(q) && !memory.SearchableText.Contains(q))
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

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _view.Refresh();

    private void AddMemory_Click(object sender, RoutedEventArgs e)
    {
        var window = new AddMemoryWindow { Owner = this };
        if (window.ShowDialog() == true && window.CreatedItem is not null)
        {
            App.Store.Add(window.CreatedItem);
            _view.Refresh();
            TimelineList.SelectedItem = window.CreatedItem;
        }
    }

    private void TimelineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Detail pane is data-bound directly to TimelineList.SelectedItem.
    }

    private void HideDetailsPane_Click(object sender, RoutedEventArgs e)
    {
        _detailsPaneVisible = false;
        DetailsPanel.Visibility = Visibility.Collapsed;
        DetailsSpacerColumn.Width = new GridLength(0);
        DetailsColumn.Width = new GridLength(0);
        ShowDetailsButton.Visibility = Visibility.Visible;
    }

    private void ShowDetailsPane_Click(object sender, RoutedEventArgs e)
    {
        _detailsPaneVisible = true;
        DetailsPanel.Visibility = Visibility.Visible;
        DetailsSpacerColumn.Width = new GridLength(12);
        DetailsColumn.Width = new GridLength(0.95, GridUnitType.Star);
        ShowDetailsButton.Visibility = Visibility.Collapsed;
    }

    private void TimelineList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TimelineList.SelectedItem is not MemoryItem)
            return;

        _detailsPaneVisible = true;
        DetailsPanel.Visibility = Visibility.Visible;
        DetailsSpacerColumn.Width = new GridLength(12);
        DetailsColumn.Width = new GridLength(0.95, GridUnitType.Star);
        ShowDetailsButton.Visibility = Visibility.Collapsed;
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MemoryItem item })
        {
            App.Store.ToggleFavorite(item);
            _view.Refresh();
        }
    }

    private void FavoriteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (TimelineList.SelectedItem is not MemoryItem item)
            return;

        App.Store.ToggleFavorite(item);
        _view.Refresh();
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
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnClosed(e);
    }
}
