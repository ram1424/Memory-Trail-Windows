using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemoryLane.Windows;

public enum SourceKind
{
    Note,
    AppActivity,
    Clipboard,
    Link,
    File,
    Screenshot
}

public sealed class MemoryItem : INotifyPropertyChanged
{
    private bool _isFavorite;
    private bool _isLiveActivity;
    private bool _isLatestActivity;

    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string SourceApp { get; set; } = "Memory Trail";
    public string SourceProcess { get; set; } = "";
    public string? SourceExecutable { get; set; }
    public SourceKind SourceKind { get; set; } = SourceKind.Note;
    public string? SourceUrl { get; set; }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite == value) return;
            _isFavorite = value;
            OnPropertyChanged();
        }
    }

    public bool IsLiveActivity
    {
        get => _isLiveActivity;
        set
        {
            if (_isLiveActivity == value) return;
            _isLiveActivity = value;
            OnPropertyChanged();
        }
    }

    public bool IsLatestActivity
    {
        get => _isLatestActivity;
        set
        {
            if (_isLatestActivity == value) return;
            _isLatestActivity = value;
            OnPropertyChanged();
        }
    }

    public string CategoryLabel
    {
        get
        {
            var app = $"{SourceApp} {SourceProcess}".ToLowerInvariant();

            if (app.Contains("edge") || app.Contains("chrome") || app.Contains("firefox") ||
                app.Contains("browser") || app.Contains("safari") || app.Contains("arc") ||
                !string.IsNullOrWhiteSpace(SourceUrl))
                return "Web";

            if (app.Contains("outlook") || app.Contains("mail"))
                return "Email";

            if (app.Contains("teams") || app.Contains("slack") || app.Contains("discord") ||
                app.Contains("zoom") || app.Contains("webex") || app.Contains("chat"))
                return "Chat";

            if (SourceKind is SourceKind.File or SourceKind.Screenshot ||
                app.Contains("explorer") || app.Contains("finder") || app.Contains("preview"))
                return "Files";

            if (app.Contains("word") || app.Contains("excel") || app.Contains("powerpoint") ||
                app.Contains("onenote") || app.Contains("notes") || app.Contains("calendar"))
                return "Productivity";

            if (app.Contains("visual studio") || app.Contains("vscode") || app.Contains("code") ||
                app.Contains("terminal") || app.Contains("powershell") || app.Contains("github") ||
                app.Contains("codex"))
                return "Development";

            if (app.Contains("music") || app.Contains("vlc") || app.Contains("photos") ||
                app.Contains("media") || app.Contains("youtube"))
                return "Media";

            if (app.Contains("figma") || app.Contains("sketch") || app.Contains("photoshop") ||
                app.Contains("illustrator") || app.Contains("canva"))
                return "Design";

            if (app.Contains("remote desktop") || app.Contains("citrix") || app.Contains("vmware") ||
                app.Contains("anydesk") || app.Contains("teamviewer"))
                return "Remote Access";

            if (app.Contains("settings") || app.Contains("control panel") || app.Contains("task manager") ||
                app.Contains("event viewer") || app.Contains("registry"))
                return "System";

            return "Other";
        }
    }

    public string ActivityLabel
    {
        get
        {
            var app = $"{SourceApp} {Title}".ToLowerInvariant();

            if (app.Contains("outlook") || app.Contains("mail")) return "Email";
            if (app.Contains("teams") || app.Contains("slack") || app.Contains("messages")) return "Chat";
            if (app.Contains("excel") || app.Contains(".xlsx")) return "Spreadsheet";
            if (app.Contains("onenote") || app.Contains("notes")) return "Notes";
            if (app.Contains("word") || app.Contains(".doc")) return "Document";
            if (app.Contains("powerpoint") || app.Contains(".ppt")) return "Presentation";
            if (CategoryLabel == "Web") return "Web";
            if (SourceKind == SourceKind.Clipboard) return "Clipboard";
            if (SourceKind == SourceKind.File) return "File";
            return CategoryLabel;
        }
    }

    public string SearchableText =>
        string.Join(" ", Title, Body, SourceApp, SourceProcess, SourceExecutable ?? "", SourceUrl ?? "")
            .ToLowerInvariant();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
