using MapManager.GUI.Models;
using MapManager.GUI.Services;
using ReactiveUI;
using System;

namespace MapManager.GUI.ViewModels;

public class LocalScoresViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;

    public LocalScoresViewModel(BeatmapDataService beatmapDataService)
    {
        _beatmapDataService = beatmapDataService;

        _beatmapDataService.OnSelectedBeatmapChanged += OnSelectedBeatmapChanged;
    }

    private void OnSelectedBeatmapChanged()
    {
        this.RaisePropertyChanged(nameof(SelectedBeatmap));
    }

    public Beatmap SelectedBeatmap => _beatmapDataService.SelectedBeatmap;
}