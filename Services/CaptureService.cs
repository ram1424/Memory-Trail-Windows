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

            string? outlookSender = null;

            if (IsOutlookProcess(processName))
            {
                // Classic Outlook can safely use the Outlook COM selection. New Outlook
                // (olk.exe) must never use that COM state because it can point at a stale
                // or completely different message in Classic Outlook.
                var outlookMessage = IsClassicOutlookProcess(processName)
                    ? TryGetClassicOutlookSelectedMessage() ?? TryGetOutlookMessageFromUiAutomation(hwnd)
                    : TryGetOutlookMessageFromUiAutomation(hwnd);

                if (outlookMessage is not null)
                {
                    title = NormalizeTitle(outlookMessage.Subject, processName);
                    outlookSender = outlookMessage.Sender;
                }
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
                    ? DetailedBody(title, processName, outlookSender)
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

    private static bool IsClassicOutlookProcess(string processName)
        => processName.Trim().Equals("outlook", StringComparison.OrdinalIgnoreCase);

    private sealed record OutlookMessageInfo(string Subject, string? Sender);

    private sealed record OutlookUiCandidate(
        string Text,
        Rect Bounds,
        string AutomationId,
        string ClassName,
        string ControlType);

    private static OutlookMessageInfo? TryGetClassicOutlookSelectedMessage()
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
            string? sender = null;

            try { subject = selectedItem.Subject as string; }
            catch { }

            try { sender = selectedItem.SenderName as string; }
            catch { }

            if (string.IsNullOrWhiteSpace(sender))
            {
                try { sender = selectedItem.SenderEmailAddress as string; }
                catch { }
            }

            subject = CleanOutlookSubject(subject);
            sender = CleanOutlookSender(sender);

            return string.IsNullOrWhiteSpace(subject)
                ? null
                : new OutlookMessageInfo(subject, sender);
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

    private static OutlookMessageInfo? TryGetOutlookMessageFromUiAutomation(IntPtr hwnd)
    {
        try
        {
            var root = AutomationElement.FromHandle(hwnd);
            if (root is null)
                return null;

            var rootBounds = SafeBounds(root);
            if (rootBounds.IsEmpty || rootBounds.Width <= 0 || rootBounds.Height <= 0)
                return null;

            var all = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            var candidates = new List<OutlookUiCandidate>();

            foreach (AutomationElement element in all)
            {
                try
                {
                    if (element.Current.IsOffscreen)
                        continue;

                    var text = CleanUiAutomationSubject(element.Current.Name);
                    if (string.IsNullOrWhiteSpace(text) || IsOutlookUiChromeText(text))
                        continue;

                    var bounds = SafeBounds(element);
                    if (bounds.IsEmpty || bounds.Width < 2 || bounds.Height < 2)
                        continue;

                    // New Outlook's selected-message reading pane is on the right side.
                    // Do not inspect the message list or left navigation because those can
                    // expose a different selected row than the message currently rendered.
                    var rightPaneStart = rootBounds.Left + (rootBounds.Width * 0.52);
                    var sufficientlyRight =
                        bounds.Left >= rightPaneStart ||
                        bounds.Right >= rootBounds.Left + (rootBounds.Width * 0.72);

                    if (!sufficientlyRight)
                        continue;

                    // Subjects and sender metadata live in the upper portion of the reading
                    // pane. Restricting the scan keeps body text and Copilot content from
                    // competing with the real message header.
                    var maxHeaderY = rootBounds.Top + Math.Max(430, rootBounds.Height * 0.58);
                    if (bounds.Top < rootBounds.Top || bounds.Top > maxHeaderY)
                        continue;

                    candidates.Add(new OutlookUiCandidate(
                        text,
                        bounds,
                        element.Current.AutomationId ?? "",
                        element.Current.ClassName ?? "",
                        element.Current.ControlType?.ProgrammaticName ?? ""));
                }
                catch
                {
                    // WebView2-backed UIA nodes can disappear between enumeration and read.
                }
            }

            if (candidates.Count == 0)
                return null;

            var subjectCandidate = candidates
                .Where(c => LooksLikeOutlookMessageSubject(c.Text))
                .OrderByDescending(c => ScoreOutlookSubjectCandidate(c, rootBounds))
                .ThenBy(c => c.Bounds.Top)
                .FirstOrDefault();

            if (subjectCandidate is null ||
                ScoreOutlookSubjectCandidate(subjectCandidate, rootBounds) < 20)
                return null;

            var senderCandidate = candidates
                .Where(c => !ReferenceEquals(c, subjectCandidate))
                .Where(c => LooksLikeOutlookSenderCandidate(c, subjectCandidate))
                .OrderByDescending(c => ScoreOutlookSenderCandidate(c, subjectCandidate))
                .ThenBy(c => Math.Abs(c.Bounds.Top - subjectCandidate.Bounds.Bottom))
                .FirstOrDefault();

            return new OutlookMessageInfo(
                subjectCandidate.Text,
                CleanOutlookSender(senderCandidate?.Text));
        }
        catch
        {
            return null;
        }
    }

    private static Rect SafeBounds(AutomationElement element)
    {
        try
        {
            return element.Current.BoundingRectangle;
        }
        catch
        {
            return Rect.Empty;
        }
    }

    private static int ScoreOutlookSubjectCandidate(OutlookUiCandidate candidate, Rect rootBounds)
    {
        var score = 0;
        var metadata = $"{candidate.AutomationId} {candidate.ClassName}".ToLowerInvariant();
        var relativeTop = (candidate.Bounds.Top - rootBounds.Top) / Math.Max(1, rootBounds.Height);

        if (metadata.Contains("subject", StringComparison.Ordinal))
            score += 140;

        if (candidate.ControlType.Contains("Text", StringComparison.OrdinalIgnoreCase) ||
            candidate.ControlType.Contains("Header", StringComparison.OrdinalIgnoreCase))
            score += 25;

        if (relativeTop <= 0.38)
            score += 35;
        else if (relativeTop <= 0.52)
            score += 12;

        if (candidate.Bounds.Width >= 120)
            score += 10;

        if (candidate.Text.Length is >= 6 and <= 180)
            score += 12;

        if (LooksLikeSenderText(candidate.Text, metadata))
            score -= 90;

        if (IsLikelyOutlookDateOrTime(candidate.Text))
            score -= 80;

        return score;
    }

    private static bool LooksLikeOutlookSenderCandidate(
        OutlookUiCandidate candidate,
        OutlookUiCandidate subject)
    {
        if (IsOutlookUiChromeText(candidate.Text) ||
            candidate.Text.Equals(subject.Text, StringComparison.OrdinalIgnoreCase))
            return false;

        var metadata = $"{candidate.AutomationId} {candidate.ClassName}".ToLowerInvariant();

        if (!LooksLikeSenderText(candidate.Text, metadata))
            return false;

        var verticalDistance = candidate.Bounds.Top - subject.Bounds.Bottom;
        return verticalDistance >= -90 && verticalDistance <= 190;
    }

    private static int ScoreOutlookSenderCandidate(
        OutlookUiCandidate candidate,
        OutlookUiCandidate subject)
    {
        var score = 0;
        var metadata = $"{candidate.AutomationId} {candidate.ClassName}".ToLowerInvariant();
        var text = candidate.Text.Trim();

        if (metadata.Contains("sender", StringComparison.Ordinal) ||
            metadata.Contains("from", StringComparison.Ordinal))
            score += 120;

        if (text.StartsWith("From:", StringComparison.OrdinalIgnoreCase))
            score += 100;

        if (text.Contains("@", StringComparison.Ordinal))
            score += 75;

        if (candidate.ControlType.Contains("Button", StringComparison.OrdinalIgnoreCase) ||
            candidate.ControlType.Contains("Hyperlink", StringComparison.OrdinalIgnoreCase) ||
            candidate.ControlType.Contains("Text", StringComparison.OrdinalIgnoreCase))
            score += 15;

        var distance = Math.Abs(candidate.Bounds.Top - subject.Bounds.Bottom);
        score += Math.Max(0, 45 - (int)Math.Min(45, distance / 2));

        return score;
    }

    private static bool LooksLikeSenderText(string text, string metadata)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        var lower = value.ToLowerInvariant();

        if (metadata.Contains("sender", StringComparison.Ordinal) ||
            metadata.Contains("from", StringComparison.Ordinal))
            return true;

        if (value.StartsWith("From:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Contains("@", StringComparison.Ordinal) &&
            !lower.StartsWith("to:", StringComparison.Ordinal) &&
            !lower.StartsWith("cc:", StringComparison.Ordinal))
            return true;

        return false;
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

    private static string? CleanOutlookSender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var sender = value.Replace("\r", " ").Replace("\n", " ").Trim();

        if (sender.StartsWith("From:", StringComparison.OrdinalIgnoreCase))
            sender = sender[5..].Trim();

        while (sender.Contains("  ", StringComparison.Ordinal))
            sender = sender.Replace("  ", " ");

        return sender.Length > 180 ? sender[..180] : sender;
    }

    private static bool LooksLikeOutlookMessageSubject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim();

        if (IsOutlookUiChromeText(text))
            return false;

        if (text.Length < 4 || text.Length > 250)
            return false;

        if (text.Contains("@", StringComparison.Ordinal) &&
            !text.Contains(" ", StringComparison.Ordinal))
            return false;

        if (text.StartsWith("From:", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("To:", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("Cc:", StringComparison.OrdinalIgnoreCase))
            return false;

        return !IsLikelyOutlookDateOrTime(text);
    }

    private static bool IsOutlookUiChromeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var lower = value.Trim().ToLowerInvariant();

        if (lower is
            "inbox" or "drafts" or "sent items" or "deleted items" or "archive" or
            "mail" or "calendar" or "people" or "tasks" or "outlook" or
            "focused" or "other" or "focused inbox" or "other inbox" or
            "summary by copilot" or "copilot" or "summarize" or
            "reply" or "reply all" or "forward" or "delete" or "move" or
            "mark as read" or "mark as unread" or "categorize" or "flag" or
            "new mail" or "search" or "filter" or "sort" or "select all")
            return true;

        if (lower.Contains("summary by copilot", StringComparison.Ordinal) ||
            lower.Contains("focused inbox selected", StringComparison.Ordinal) ||
            lower.Contains("focused inbox unselected", StringComparison.Ordinal) ||
            lower.Contains("other inbox selected", StringComparison.Ordinal) ||
            lower.Contains("other inbox unselected", StringComparison.Ordinal) ||
            lower.Contains("reading pane", StringComparison.Ordinal) ||
            lower.Contains("message list", StringComparison.Ordinal) ||
            lower.Contains("navigation pane", StringComparison.Ordinal) ||
            lower.Contains("draft with copilot", StringComparison.Ordinal) ||
            lower.StartsWith("you don't often get email from", StringComparison.Ordinal) ||
            lower.StartsWith("external sender", StringComparison.Ordinal))
            return true;

        if (lower.EndsWith(" selected", StringComparison.Ordinal) ||
            lower.EndsWith(" unselected", StringComparison.Ordinal))
            return true;

        return false;
    }

    private static bool IsLikelyOutlookDateOrTime(string value)
    {
        var text = value.Trim().ToLowerInvariant();

        if (text.Length > 80)
            return false;

        if (text.Contains(" am", StringComparison.Ordinal) ||
            text.Contains(" pm", StringComparison.Ordinal) ||
            text.EndsWith("am", StringComparison.Ordinal) ||
            text.EndsWith("pm", StringComparison.Ordinal))
            return true;

        string[] dayPrefixes =
        [
            "monday", "tuesday", "wednesday", "thursday",
            "friday", "saturday", "sunday", "today", "yesterday"
        ];

        return dayPrefixes.Any(day =>
            text.Equals(day, StringComparison.Ordinal) ||
            text.StartsWith(day + ",", StringComparison.Ordinal) ||
            text.StartsWith(day + " ", StringComparison.Ordinal));
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

    private static string DetailedBody(string title, string processName, string? outlookSender = null)
    {
        var p = processName.ToLowerInvariant();

        if (p.Contains("outlook") || p == "olk")
        {
            return string.IsNullOrWhiteSpace(outlookSender)
                ? "Subject: " + title
                : "From: " + outlookSender + Environment.NewLine + "Subject: " + title;
        }

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
