using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MemoryLane.Windows;

public partial class App : Application
{
    public static PreferencesService Preferences { get; } = new();
    public static TimelineStore Store { get; } = new();
    public static CaptureService Capture { get; } = new(Store, Preferences);

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        if (string.Equals(
                Environment.GetEnvironmentVariable("MEMORY_TRAIL_EXPORT_SCREENSHOTS"),
                "1",
                StringComparison.Ordinal))
        {
            try
            {
                Preferences.HasCompletedOnboarding = true;
                var outputDirectory =
                    Environment.GetEnvironmentVariable("MEMORY_TRAIL_SCREENSHOT_DIR")
                    ?? Path.Combine(Environment.CurrentDirectory, "docs", "images", "screenshots");
                ScreenshotExporter.ExportAll(outputDirectory);
            }
            catch (Exception ex)
            {
                LogNonFatal("ScreenshotExporter", ex);
                Environment.ExitCode = 1;
            }

            Shutdown();
            return;
        }

        SplashWindow? splash = null;
        var startupTimer = Stopwatch.StartNew();

        try
        {
            splash = new SplashWindow();
            MainWindow = splash;
            splash.Show();

            await Dispatcher.Yield(DispatcherPriority.Loaded);

            splash.SetStatus("Loading preferences…");
            await Task.Run(() => Preferences.Load());

            splash.SetStatus("Restoring your timeline…");
            await Task.Run(() => Store.Load());

            if (AppVersion.IsMicrosoftStoreBuild)
            {
                UpdateService.ClearPendingUpdate(Preferences);
                Preferences.Save();
            }
            else
            {
                UpdateService.ReconcilePendingUpdate(Preferences);
            }

            splash.SetStatus("Preparing your workspace…");
            await Dispatcher.Yield(DispatcherPriority.Background);

            var window = new MainWindow();
            WindowsParityEnhancements.Attach(window);

            var minimumSplashTime = TimeSpan.FromSeconds(2);
            var remaining = minimumSplashTime - startupTimer.Elapsed;
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining);

            splash.SetStatus("Ready");

            MainWindow = window;
            window.Show();
            splash.Close();
            splash = null;

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                try { Capture.Start(); }
                catch (Exception ex) { LogNonFatal("Capture.Start", ex); }
            }));
        }
        catch (Exception ex)
        {
            try { splash?.Close(); } catch { }

            try
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Memory Trail");
                Directory.CreateDirectory(folder);
                File.AppendAllText(
                    Path.Combine(folder, "crash.log"),
                    $"[{DateTimeOffset.Now:O}]\r\n{ex}\r\n\r\n");
            }
            catch { }

            MessageBox.Show(
                "Memory Trail could not start. A crash log was saved under Local AppData > Memory Trail > crash.log.",
                "Memory Trail",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    public static void LogNonFatal(string component, Exception ex)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Memory Trail");
            Directory.CreateDirectory(folder);
            File.AppendAllText(
                Path.Combine(folder, "crash.log"),
                $"[{DateTimeOffset.Now:O}] NON-FATAL {component}\r\n{ex}\r\n\r\n");
        }
        catch { }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Capture.Stop();
        Preferences.Save();
        Store.Save();

        if (!AppVersion.IsMicrosoftStoreBuild)
        {
            try
            {
                UpdateService.SchedulePendingInstall(Preferences);
            }
            catch (Exception ex)
            {
                LogNonFatal("Schedule pending update", ex);
            }
        }

        base.OnExit(e);
    }
}
