using System.Windows;

namespace MemoryLane.Windows;

public partial class App : Application
{
    public static PreferencesService Preferences { get; } = new();
    public static TimelineStore Store { get; } = new();
    public static CaptureService Capture { get; } = new(Store, Preferences);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            Preferences.Load();
            Store.Load();

            var window = new MainWindow();
            MainWindow = window;
            window.Show();

            window.Dispatcher.BeginInvoke(new Action(() =>
            {
                try { Capture.Start(); }
                catch (Exception ex) { LogNonFatal("Capture.Start", ex); }
            }));
        }
        catch (Exception ex)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Memory Trail");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "crash.log"), $"[{DateTimeOffset.Now:O}]\r\n{ex}\r\n\r\n");
            }
            catch { }

            MessageBox.Show("Memory Trail could not start. A crash log was saved under Local AppData > Memory Trail > crash.log.", "Memory Trail", MessageBoxButton.OK, MessageBoxImage.Error);
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
        base.OnExit(e);
    }
}
