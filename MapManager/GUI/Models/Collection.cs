using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace MapManager.GUI.Models;

public class Collection : ReactiveObject
{
    public string Name { get; set; }
    public int Count { get; set; }

    private ObservableCollection<Beatmap> _beatmaps = new();
    public ObservableCollection<Beatmap> Beatmaps
    {
        get => _beatmaps;
        set
        {
            if (_beatmaps != null)
                _beatmaps.CollectionChanged -= OnSubCollectionChanged;
            this.RaiseAndSetIfChanged(ref _beatmaps, value);
            if (_beatmaps != null)
                _beatmaps.CollectionChanged += OnSubCollectionChanged;
            RebuildAllItems();
        }
    }

    public ObservableCollection<MissingBeatmap> MissingBeatmaps { get; } = new();

    // Flat list combining Beatmap + MissingBeatmap for TreeView binding
    public ObservableCollection<object> AllItems { get; } = new();

    public int MissingCount => MissingBeatmaps.Count;
    public bool HasMissing => MissingBeatmaps.Count > 0;

    public Collection()
    {
        MissingBeatmaps.CollectionChanged += OnSubCollectionChanged;
        MissingBeatmaps.CollectionChanged += (_, __) =>
        {
            this.RaisePropertyChanged(nameof(MissingCount));
            this.RaisePropertyChanged(nameof(HasMissing));
        };
    }

    private void OnSubCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RebuildAllItems();

    private void RebuildAllItems()
    {
        AllItems.Clear();
        foreach (var b in _beatmaps)
            AllItems.Add(b);
        foreach (var m in MissingBeatmaps)
            AllItems.Add(m);
    }
}
