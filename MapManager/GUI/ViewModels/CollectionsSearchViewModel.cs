using MapManager.GUI.Services;
using System;

namespace MapManager.GUI.ViewModels;

public class CollectionsSearchViewModel : ViewModelBase
{
    private readonly BeatmapService _beatmapService;
    private readonly BeatmapDataService _beatmapDataService;
    public CollectionsSearchViewModel(BeatmapService beatmapService, BeatmapDataService beatmapDataService)
    {
        _beatmapService = beatmapService;
        _beatmapDataService = beatmapDataService;
    }

    public void AddCollection()
    {
        _beatmapService.AddCollection(Random.Shared.Next(0, 99999999).ToString(), new() { _beatmapDataService.SelectedBeatmap.MD5Hash });
    }
}