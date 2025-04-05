using DynamicData;
using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;

public class BeatmapBlockCollectionsViewModel : ViewModelBase
{
    private readonly BeatmapService _beatmapService;
    private readonly BeatmapDataService _beatmapDataService;
    public BeatmapBlockCollectionsViewModel(BeatmapService beatmapService, BeatmapDataService beatmapDataService)
    {
        _beatmapService = beatmapService;
        _beatmapDataService = beatmapDataService;

        SelectedBeatmapCollections.CollectionChanged += SelectedBeatmapCollections_CollectionChanged;
        _beatmapDataService.OnSelectedBeatmapChanged += OnSelectedBeatmapChanged;
    }

    private void SelectedBeatmapCollections_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null && e.NewItems.Count > 0)
            foreach (var item in e.NewItems)
            {
                _beatmapService.AddToCollection(((Collection)item).Name, _beatmapDataService.SelectedBeatmap);
            }

        if (e.OldItems is not null && e.OldItems.Count > 0)
            foreach (var item in e.OldItems)
            {
                _beatmapService.RemoveFromCollection(((Collection)item).Name, _beatmapDataService.SelectedBeatmap);
            }
    }

    public ObservableCollection<Collection> Collections => _beatmapDataService.Collections;

    public ObservableCollection<Collection> SelectedBeatmapCollections { get; set; } = new();






    private void OnSelectedBeatmapChanged()
    {
        this.RaisePropertyChanged(nameof(Collections));
        var beatmapData = _beatmapService.GetBeatmapPresentationData(_beatmapDataService.SelectedBeatmap);
        SelectedBeatmapCollections.Clear();
        SelectedBeatmapCollections.AddRange(beatmapData.collections);

    }
}