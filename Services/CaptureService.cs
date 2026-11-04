using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Threading;

namespace MemoryLane.Windows;

public sealed class CaptureService
{
    private readonly TimelineStore _store;
    private readonly PreferencesService _preferences;
    private DispatcherTimer? _windowTimer;
    private DispatcherTimer? _clipboardTimer;
    private IntPtr _foregroundHook;
    private WinEventDelegate? _foregroundDelegate;
    private bool _started;
    private string _lastWindowSignature = "";
    private string _lastClipboardText = "";

    public bool IsRunning => _started && _windowTimer?.IsEnabled == true;
    public DateTimeOffset? LastActivityCapturedAt { get; private set; }

    public CaptureService(TimelineStore store, PreferencesService preferences)
    {
        _store = store;
        _preferences = preferences;
    }

    public void Start()
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (!dispatcher.CheckAccess())
        {
            dispatcher.Invoke(Start);
            return;
        }

        if (_started && _windowTimer?.IsEnabled == true)
            return;

        try
        {
            _windowTimer ??= new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _windowTimer.Tick -= WindowTimer_Tick;
            _windowTimer.Tick += WindowTimer_Tick;

            _clipboardTimer ??= new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1.5)
            };
            _clipboardTimer.Tick -= ClipboardTimer_Tick;
            _clipboardTimer.Tick += ClipboardTimer_Tick;

            _foregroundDelegate ??= ForegroundChanged;
            if (_foregroundHook == IntPtr.Zero)
            {
                _foregroundHook = SetWinEventHook(
                    EventSystemForeground,
                    EventSystemForeground,
                    IntPtr.Zero,
                    _foregroundDelegate,
                    0,
                    0,
                    WinEventOutOfContext | WinEventSkipOwnProcess);
            }

            _windowTimer.Start();
            _clipboardTimer.Start();
            _started = true;

            // Capture immediately instead of waiting for the first timer tick.
            SafeCaptureWindow(GetForegroundWindow());
        }
        catch (Exception ex)
        {
            _started = false;
            App.LogNonFatal("CaptureService.Start", ex);
        }
    }

    public void Stop()
    {
        try
        {
            _windowTimer?.Stop();
            _clipboardTimer?.Stop();

            if (_foregroundHook != IntPtr.Zero)
            {
                _ = UnhookWinEvent(_foregroundHook);
                _foregroundHook = IntPtr.Zero;
            }
        }
        catch (Exception ex)
        {
            App.LogNonFatal("CaptureService.Stop", ex);
        }
        finally
        {
            _started = false;
        }
    }

    private void WindowTimer_Tick(object? sender, EventArgs e) => SafeCaptureWindow(GetForegroundWindow());

    private void ClipboardTimer_Tick(object? sender, EventArgs e)
    {
        try { PollClipboard(); }
        catch (Exception ex) { App.LogNonFatal("CaptureService.PollClipboard", ex); }
    }

    private void ForegroundChanged(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime)
    {
        if (hwnd == IntPtr.Zero)
            return;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
            return;

        dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => SafeCaptureWindow(hwnd)));
    }

    public void ClearRuntimeCache()
    {
        _lastWindowSignature = "";
        _lastClipboardText = "";
    }

    private void SafeCaptureWindow(IntPtr hwnd)
    {
        try
        {
            CaptureWindow(hwnd);
        }
        catch (Exception ex)
        {
            App.LogNonFatal("CaptureService.CaptureWindow", ex);
        }
    }

    private void CaptureWindow(IntPtr hwnd)
    {
        if (_preferences.CapturePaused || !_preferences.CaptureAppActivity)
            return;

        if (hwnd == IntPtr.Zero)
            return;

        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0 || pid == Environment.ProcessId)
            return;

        Process process;
        try { process = Process.GetProcessById((int)pid); }
        catch { return; }

        using (process)
        {
            var processName = process.ProcessName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(processName) || _preferences.IsExcluded(processName))
                return;

            if (IsMemoryTrailProcess(processName))
                return;

            var friendlyApp = FriendlyAppName(processName);
            var rawTitle = GetWindowTitle(hwnd);

            if (string.IsNullOrWhiteSpace(rawTitle))
            {
                try { rawTitle = process.MainWindowTitle; }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(rawTitle))
                rawTitle = TryGetAutomationWindowName(hwnd) ?? "";

            var title = NormalizeTitle(rawTitle, processName);

            if (IsOutlookProcess(processName))
            {
                var outlookSubject = TryGetClassicOutlookSelectedSubject()
                    ?? TryGetOutlookSubjectFromUiAutomation(hwnd);

                if (!string.IsNullOrWhiteSpace(outlookSubject))
                    title = NormalizeTitle(outlookSubject, processName);
            }

            var hasUsefulTitle = !string.IsNullOrWhiteSpace(title) && !IsGeneric(title, processName);
            if (!hasUsefulTitle)
                title = $"Opened {friendlyApp}";

            var signature = (processName + "|" + title).ToLowerInvariant();
            if (signature == _lastWindowSignature)
                return;

            _lastWindowSignature = signature;

            _store.Add(new MemoryItem
            {
                Title = title,
                Body = hasUsefulTitle
                    ? DetailedBody(title, processName)
                    : $"Application became active: {friendlyApp}",
                SourceApp = friendlyApp,
                SourceProcess = processName,
                SourceExecutable = GetExecutablePath(process),
                SourceKind = SourceKind.AppActivity
            });

            LastActivityCapturedAt = DateTimeOffset.Now;
        }
    }

    private static bool IsMemoryTrailProcess(string processName)
    {
        var value = processName.Replace(" ", "", StringComparison.Ordinal)
            .Replace(".", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal);

        return value.Equals("MemoryTrail", StringComparison.OrdinalIgnoreCase)
            || value.Equals("MemoryLane", StringComparison.OrdinalIgnoreCase)
            || value.Equals("MemoryLaneWindows", StringComparison.OrdinalIgnoreCase)
            || value.Equals("MemoryTrailWindows", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryGetAutomationWindowName(IntPtr hwnd)
    {
        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            var name = root?.Current.Name?.Trim();
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch
        {
            return null;
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

        if (!string.IsNullOrWhiteSpace(processName) && _preferences.IsExcluded(processName)) return;

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

        LastActivityCapturedAt = DateTimeOffset.Now;
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
            or "inbox" or "mail" or "calendar"
            || value == processName.ToLowerInvariant();
    }

    private static bool IsOutlookProcess(string processName)
    {
        var value = processName.Trim().ToLowerInvariant();
        return value == "olk" || value.Contains("outlook", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryGetClassicOutlookSelectedSubject()
    {
        object? app = null;
        object? explorer = null;
        object? selection = null;
        object? item = null;

        try
        {
            var outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType is null)
                return null;

            app = GetActiveComObject("Outlook.Application");
            if (app is null)
                return null;

            dynamic outlook = app;
            explorer = outlook.ActiveExplorer();
            if (explorer is null)
                return null;

            dynamic activeExplorer = explorer;
            selection = activeExplorer.Selection;
            if (selection is null)
                return null;

            dynamic selected = selection;
            if ((int)selected.Count < 1)
                return null;

            item = selected.Item(1);
            if (item is null)
                return null;

            dynamic selectedItem = item;

            string? subject = null;
            try { subject = selectedItem.Subject as string; }
            catch { }

            subject = CleanOutlookSubject(subject);
            return string.IsNullOrWhiteSpace(subject) ? null : subject;
        }
        catch
        {
            return null;
        }
        finally
        {
            ReleaseComObject(item);
            ReleaseComObject(selection);
            ReleaseComObject(explorer);
            ReleaseComObject(app);
        }
    }

    private static string? TryGetOutlookSubjectFromUiAutomation(IntPtr hwnd)
    {
        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            if (root is null)
                return null;

            var selectedRows = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.IsSelectionItemPatternAvailableProperty,
                    true));

            for (var i = 0; i < selectedRows.Count; i++)
            {
                var element = selectedRows[i];

                try
                {
                    if (element.GetCurrentPattern(SelectionItemPattern.Pattern) is not SelectionItemPattern pattern ||
                        !pattern.Current.IsSelected)
                        continue;

                    var candidate = CleanUiAutomationSubject(element.Current.Name);
                    if (LooksLikeOutlookMessageSubject(candidate))
                        return candidate;
                }
                catch { }
            }

            var textElements = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.ControlTypeProperty,
                    ControlType.Text));

            foreach (AutomationElement element in textElements)
            {
                var candidate = CleanUiAutomationSubject(element.Current.Name);
                if (LooksLikeOutlookMessageSubject(candidate))
                    return candidate;
            }
        }
        catch { }

        return null;
    }

    private static string? CleanUiAutomationSubject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Replace("\r", " ").Replace("\n", " ").Trim();
        while (text.Contains("  ", StringComparison.Ordinal))
            text = text.Replace("  ", " ");

        if (text.Length > 250)
            text = text[..250];

        return text;
    }

    private static string? CleanOutlookSubject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var subject = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return subject.Length > 250 ? subject[..250] : subject;
    }

    private static bool LooksLikeOutlookMessageSubject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();
        var lower = text.ToLowerInvariant();

        if (lower is "inbox" or "drafts" or "sent items" or "deleted items" or "archive"
            or "mail" or "calendar" or "people" or "tasks" or "outlook")
            return false;

        if (text.Length < 4 || text.Length > 250)
            return false;

        if (text.Contains("@") && !text.Contains(" ", StringComparison.Ordinal))
            return false;

        return true;
    }

    private static object? GetActiveComObject(string progId)
    {
        try
        {
            var clsid = Type.GetTypeFromProgID(progId)?.GUID ?? Guid.Empty;
            if (clsid == Guid.Empty)
                return null;

            GetActiveObject(ref clsid, IntPtr.Zero, out var result);
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is null)
            return;

        try
        {
            if (Marshal.IsComObject(value))
                Marshal.FinalReleaseComObject(value);
        }
        catch { }
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

    private static string FriendlyAppName(string processName)
    {
        var value = processName.Trim();
        var key = value.ToLowerInvariant();

        if (key == "olk" || key.Contains("outlook", StringComparison.OrdinalIgnoreCase))
            return "Microsoft Outlook";

        return key switch
        {
            "msedge" => "Microsoft Edge",
            "chrome" => "Google Chrome",
            "ms-teams" or "teams" => "Microsoft Teams",
            "explorer" => "File Explorer",
            _ => string.IsNullOrWhiteSpace(value) ? "Application" : value
        };
    }

    private const uint EventSystemForeground = 0x0003;
    private const uint WinEventOutOfContext = 0x0000;
    private const uint WinEventSkipOwnProcess = 0x0002;

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint eventThread,
        uint eventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
}
