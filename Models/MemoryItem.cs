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

    public string SearchableText =>
        string.Join(" ", Title, Body, SourceApp, SourceProcess, SourceExecutable ?? "", SourceUrl ?? "")
            .ToLowerInvariant();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
