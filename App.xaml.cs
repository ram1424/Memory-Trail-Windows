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

        Preferences.Load();
        Store.Load();

        var window = new MainWindow();
        MainWindow = window;
        window.Show();

        Capture.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Capture.Stop();
        Preferences.Save();
        Store.Save();
        base.OnExit(e);
    }
}
