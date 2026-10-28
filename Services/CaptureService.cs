using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace MemoryLane.Windows;

public sealed class CaptureService
{
    private readonly TimelineStore _store;
    private readonly PreferencesService _preferences;
    private readonly DispatcherTimer _windowTimer;
    private readonly DispatcherTimer _clipboardTimer;
    private string _lastWindowSignature = "";
    private string _lastClipboardText = "";

    public CaptureService(TimelineStore store, PreferencesService preferences)
    {
        _store = store;
        _preferences = preferences;

        _windowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _windowTimer.Tick += (_, _) => PollForegroundWindow();

        _clipboardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _clipboardTimer.Tick += (_, _) => PollClipboard();
    }

    public void Start()
    {
        _windowTimer.Start();
        _clipboardTimer.Start();
    }

    public void Stop()
    {
        _windowTimer.Stop();
        _clipboardTimer.Stop();
    }

    public void ClearRuntimeCache()
    {
        _lastWindowSignature = "";
        _lastClipboardText = "";
    }

    private void PollForegroundWindow()
    {
        if (_preferences.CapturePaused || !_preferences.CaptureAppActivity) return;

        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return;
        _ = GetWindowThreadProcessId(hwnd, out var pid);

        Process process;
        try { process = Process.GetProcessById((int)pid); }
        catch { return; }

        using (process)
        {
        var processName = process.ProcessName;
        if (_preferences.IsExcluded(processName)) return;
        if (processName.Equals("Memory Trail", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("Memory Lane", StringComparison.OrdinalIgnoreCase) ||
            processName.Equals("MemoryLane.Windows", StringComparison.OrdinalIgnoreCase))
            return;

        var title = NormalizeTitle(GetWindowTitle(hwnd), processName);
        if (string.IsNullOrWhiteSpace(title) || IsGeneric(title, processName)) return;

        var signature = (processName + "|" + title).ToLowerInvariant();
        if (signature == _lastWindowSignature) return;
        _lastWindowSignature = signature;

        _store.Add(new MemoryItem
        {
            Title = title,
            Body = DetailedBody(title, processName),
            SourceApp = FriendlyAppName(processName),
            SourceProcess = processName,
            SourceExecutable = GetExecutablePath(process),
            SourceKind = SourceKind.AppActivity
        });
        }
    }

    private void PollClipboard()
    {
        if (_preferences.CapturePaused || !_preferences.CaptureClipboard) return;

        string text;
        try
        {
            if (!Clipboard.ContainsText()) return;
            text = Clipboard.GetText().Trim();
        }
        catch { return; }

        if (string.IsNullOrWhiteSpace(text) || text.Length > 20000 || text == _lastClipboardText) return;
        _lastClipboardText = text;

        var hwnd = GetForegroundWindow();
        _ = GetWindowThreadProcessId(hwnd, out var pid);

        var processName = "";
        string? executablePath = null;
        try
        {
            using var process = Process.GetProcessById((int)pid);
            processName = process.ProcessName;
            executablePath = GetExecutablePath(process);
        }
        catch { }

        if (_preferences.IsExcluded(processName)) return;

        var preview = text.Replace("\r", " ").Replace("\n", " ");
        _store.Add(new MemoryItem
        {
            Title = preview.Length > 90 ? preview[..90] : preview,
            Body = text,
            SourceApp = FriendlyAppName(processName),
            SourceProcess = processName,
            SourceExecutable = executablePath,
            SourceKind = SourceKind.Clipboard,
            SourceUrl = Uri.TryCreate(text, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.ToString()
                : null
        });
    }

    private static string? GetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        if (length <= 0) return "";

        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string NormalizeTitle(string raw, string processName)
    {
        var title = raw.Replace("\r", " ").Replace("\n", " ").Trim();

        string[] suffixes =
        [
            " - Google Chrome",
            " — Google Chrome",
            " - Microsoft Edge",
            " — Microsoft Edge",
            " - Outlook",
            " — Outlook",
            " | Microsoft Teams",
            " - Microsoft Teams",
            " — Microsoft Teams"
        ];

        foreach (var suffix in suffixes)
        {
            if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                title = title[..^suffix.Length].Trim();
        }

        return title.Length > 250 ? title[..250] : title;
    }

    private static bool IsGeneric(string title, string processName)
    {
        var value = title.Trim().ToLowerInvariant();
        return value is "" or "new tab" or "home" or "settings" or "preferences" or "untitled"
            || value == processName.ToLowerInvariant();
    }

    private static string DetailedBody(string title, string processName)
    {
        var p = processName.ToLowerInvariant();

        if (p.Contains("outlook") || p == "olk") return "Outlook item viewed: " + title;
        if (p.Contains("teams")) return "Teams window viewed: " + title;
        if (p.Contains("chrome")) return "Chrome page viewed: " + title;
        if (p.Contains("msedge")) return "Edge page viewed: " + title;
        if (p == "explorer") return "File Explorer window viewed: " + title;

        return "Focused window in " + FriendlyAppName(processName) + ": " + title;
    }

    private static string FriendlyAppName(string processName) => processName.ToLowerInvariant() switch
    {
        "msedge" => "Microsoft Edge",
        "chrome" => "Google Chrome",
        "outlook" or "olk" => "Microsoft Outlook",
        "ms-teams" or "teams" => "Microsoft Teams",
        "explorer" => "File Explorer",
        _ => string.IsNullOrWhiteSpace(processName) ? "Application" : processName
    };

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);
}
