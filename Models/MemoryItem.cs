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

public sealed class MemoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string SourceApp { get; set; } = "Memory Trail";
    public string SourceProcess { get; set; } = "";
    public string? SourceExecutable { get; set; }
    public SourceKind SourceKind { get; set; } = SourceKind.Note;
    public string? SourceUrl { get; set; }
    public bool IsFavorite { get; set; }

    public string SearchableText =>
        string.Join(" ", Title, Body, SourceApp, SourceProcess, SourceExecutable ?? "", SourceUrl ?? "")
            .ToLowerInvariant();
}
