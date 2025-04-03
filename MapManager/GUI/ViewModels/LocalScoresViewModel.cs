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
        this.RaisePropertyChanged(nameof(IsSelectedBeatmapScoresEmpty));
    }

    public Beatmap SelectedBeatmap => _beatmapDataService.SelectedBeatmap;
    public bool IsSelectedBeatmapScoresEmpty => SelectedBeatmap.Scores is null || SelectedBeatmap.Scores.Count == 0;
}