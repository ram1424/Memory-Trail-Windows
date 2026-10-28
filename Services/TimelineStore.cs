using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryLane.Windows;

public sealed class TimelineStore
{
    private readonly object _gate = new();
    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Memory Trail");
    private readonly string _legacyFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Memory Lane");
    private string FilePath => Path.Combine(_folder, "timeline.json");
    private CancellationTokenSource? _saveCts;

    public ObservableCollection<MemoryItem> Items { get; } = [];

    public void Add(MemoryItem item)
    {
        App.Current.Dispatcher.Invoke(() => Items.Insert(0, item));
        ScheduleSave();
    }

    public void Delete(MemoryItem item)
    {
        App.Current.Dispatcher.Invoke(() => Items.Remove(item));
        ScheduleSave();
    }

    public void Clear()
    {
        App.Current.Dispatcher.Invoke(() => Items.Clear());
        ScheduleSave();
    }

    public void ToggleFavorite(MemoryItem item)
    {
        App.Current.Dispatcher.Invoke(() => item.IsFavorite = !item.IsFavorite);
        ScheduleSave();
    }

    public void Load()
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                MigrateLegacyTimeline();
                if (!File.Exists(FilePath)) return;
                var items = JsonSerializer.Deserialize<List<MemoryItem>>(File.ReadAllText(FilePath)) ?? [];
                App.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var item in items.OrderByDescending(x => x.CreatedAt))
                        Items.Add(item);
                });
            }
            catch (Exception ex)
            {
                App.LogNonFatal("TimelineStore.Load", ex);
            }
        }
    }

    private void MigrateLegacyTimeline()
    {
        try
        {
            var legacyFile = Path.Combine(_legacyFolder, "timeline.json");
            if (!File.Exists(FilePath) && File.Exists(legacyFile))
                File.Copy(legacyFile, FilePath, false);
        }
        catch (Exception ex)
        {
            App.LogNonFatal("TimelineStore.MigrateLegacyTimeline", ex);
        }
    }

    private void ScheduleSave()
    {
        var next = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _saveCts, next);
        previous?.Cancel();
        previous?.Dispose();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(700, next.Token);
                if (!next.IsCancellationRequested)
                    Save();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                App.LogNonFatal("TimelineStore.ScheduleSave", ex);
            }
        });
    }

    public void Save()
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                var snapshot = App.Current.Dispatcher.Invoke(() => Items.ToList());
                var temp = FilePath + ".tmp";
                File.WriteAllText(
                    temp,
                    JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false }));
                File.Move(temp, FilePath, true);
            }
            catch (Exception ex)
            {
                App.LogNonFatal("TimelineStore.Save", ex);
            }
        }
    }
}
