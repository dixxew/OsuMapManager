using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace MapManager.GUI.ViewModels;
public class BeatmapsViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;

    public BeatmapsViewModel(BeatmapDataService beatmapDataService)
    {
        _beatmapDataService = beatmapDataService;
        _beatmapDataService.OnSelectedBeatmapSetChanged += OnSelectedBeatmapSetChanged;
        _beatmapDataService.OnLoadingChanged += () => this.RaisePropertyChanged(nameof(IsLoading));
    }

    public bool IsLoading => _beatmapDataService.IsLoading;

    public BeatmapSet SelectedBeatmapSet
    {
        get => _beatmapDataService.SelectedBeatmapSet;
        set
        {
            if (value is not null)
                _beatmapDataService.SelectBeatmapSetAndBeatmap(value);
        }
    }

    public ObservableCollection<BeatmapSet> FilteredBeatmapSets => _beatmapDataService.FilteredBeatmapSets;

    private void OnSelectedBeatmapSetChanged()
    {
        this.RaisePropertyChanged(nameof(SelectedBeatmapSet));
        SelectedBeatmapSetChanged?.Invoke();
    }

    public Action SelectedBeatmapSetChanged;
}
