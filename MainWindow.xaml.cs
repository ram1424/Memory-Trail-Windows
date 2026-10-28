using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace MemoryLane.Windows;

public partial class MainWindow : Window
{
    private readonly ICollectionView _view;
    private readonly Forms.NotifyIcon _trayIcon;
    private bool _quitting;
    private string _mode = "Timeline";
    private bool _initializingSettings = true;

    public MainWindow()
    {
        InitializeComponent();

        _view = CollectionViewSource.GetDefaultView(App.Store.Items);
        _view.Filter = FilterItem;
        TimelineList.ItemsSource = _view;

        LoadSettingsIntoUi();
        UpdateCaptureState();

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Memory Lane",
            Visible = true,
            Icon = CreateTrayIcon()
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Memory Lane", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        menu.Items.Add("Pause / Resume Capture", null, (_, _) => Dispatcher.Invoke(ToggleCapture));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(QuitApplication));
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);

        Closing += OnClosing;
        Loaded += MainWindow_Loaded;

        _initializingSettings = false;
    }

    private static System.Drawing.Icon CreateTrayIcon()
    {
        try
        {
            var resource = Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/MemoryLaneWindowsClockIcon.png"));
            if (resource?.Stream is null)
                return System.Drawing.SystemIcons.Application;

            using var bitmap = new System.Drawing.Bitmap(resource.Stream);
            var handle = bitmap.GetHicon();
            try
            {
                using var icon = System.Drawing.Icon.FromHandle(handle);
                return (System.Drawing.Icon)icon.Clone();
            }
            finally
            {
                DestroyIcon(handle);
            }
        }
        catch
        {
            return System.Drawing.SystemIcons.Application;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        if (!App.Preferences.HasCompletedOnboarding)
            ShowOnboarding();
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
        _trayIcon.Visible = false;
        Application.Current.Shutdown();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string mode) return;

        _mode = mode;
        PageTitle.Text = mode;
        SearchBox.Visibility = mode == "Settings" ? Visibility.Collapsed : Visibility.Visible;
        SettingsPanel.Visibility = mode == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        TimelineContainer.Visibility = mode == "Settings" ? Visibility.Collapsed : Visibility.Visible;
        ClearTimelineButton.Visibility = mode == "Timeline" ? Visibility.Visible : Visibility.Collapsed;

        PageSubtitle.Text = mode switch
        {
            "Timeline" => "Your recent activity",
            "Search" => "Find anything Memory Lane remembers",
            "Favorites" => "Keep important memories close",
            "Settings" => "Control what Memory Lane remembers and how it behaves",
            _ => ""
        };

        _view.Refresh();
    }

    private bool FilterItem(object item)
    {
        if (item is not MemoryItem memory) return false;

        if (_mode == "Favorites" && !memory.IsFavorite)
            return false;

        var q = SearchBox.Text?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(q) && !memory.SearchableText.Contains(q))
            return false;

        return true;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _view.Refresh();

    private void AddMemory_Click(object sender, RoutedEventArgs e)
    {
        var window = new AddMemoryWindow { Owner = this };
        if (window.ShowDialog() == true && window.CreatedItem is not null)
            App.Store.Add(window.CreatedItem);
    }

    private void TimelineList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TimelineList.SelectedItem is MemoryItem item)
            ShowDetails(item);
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MemoryItem item })
            ShowDetails(item);
    }

    private void ShowDetails(MemoryItem item)
    {
        var window = new MemoryDetailWindow(item) { Owner = this };
        window.ShowDialog();
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MemoryItem item })
        {
            App.Store.ToggleFavorite(item);
            _view.Refresh();
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MemoryItem item })
            App.Store.Delete(item);
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
        StatusDot.Fill = App.Preferences.CapturePaused ? Brushes.Orange : Brushes.LimeGreen;
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
        MessageBox.Show("Exclusions updated.", "Memory Lane");
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
        MessageBox.Show("App cache and preferences were reset. Timeline history was not deleted.", "Memory Lane");
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnClosed(e);
    }
}
