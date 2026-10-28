using System.Collections.ObjectModel;
using System.Text.Json;

namespace MemoryLane.Windows;

public sealed class TimelineStore
{
    private readonly object _gate = new();
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Memory Lane");
    private string FilePath => Path.Combine(_folder, "timeline.json");

    public ObservableCollection<MemoryItem> Items { get; } = [];

    public void Add(MemoryItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Items.Insert(0, item);
            Save();
        });
    }

    public void Delete(MemoryItem item)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Items.Remove(item);
            Save();
        });
    }

    public void Clear()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Items.Clear();
            Save();
        });
    }

    public void ToggleFavorite(MemoryItem item)
    {
        item.IsFavorite = !item.IsFavorite;
        Save();
    }

    public void Load()
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                if (!File.Exists(FilePath)) return;
                var items = JsonSerializer.Deserialize<List<MemoryItem>>(File.ReadAllText(FilePath)) ?? [];
                App.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var item in items.OrderByDescending(x => x.CreatedAt))
                        Items.Add(item);
                });
            }
            catch { }
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                var snapshot = App.Current.Dispatcher.Invoke(() => Items.ToList());
                File.WriteAllText(
                    FilePath,
                    JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false }));
            }
            catch { }
        }
    }
}
