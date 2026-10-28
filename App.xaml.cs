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

            Capture.Start();
        }
        catch (Exception ex)
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Memory Lane");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "crash.log"), $"[{DateTimeOffset.Now:O}]\r\n{ex}\r\n\r\n");
            }
            catch { }

            MessageBox.Show("Memory Lane could not start. A crash log was saved under Local AppData > Memory Lane > crash.log.", "Memory Lane", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Capture.Stop();
        Preferences.Save();
        Store.Save();
        base.OnExit(e);
    }
}
