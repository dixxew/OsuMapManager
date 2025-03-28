using MapManager.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;
public class BeatmapsSearchViewModel : ViewModelBase
{
    private readonly BeatmapDataService _beatmapDataService;

    public BeatmapsSearchViewModel(BeatmapDataService beatmapDataService)
    {
        _beatmapDataService = beatmapDataService;
    }

    private string _searchBeatmapSetText;
    public string SearchBeatmapSetText
    {
        get => _searchBeatmapSetText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchBeatmapSetText, value);
            PerformSearch();
        }
    }

    private bool _isShowOnlyFavorites;
    public bool IsShowOnlyFavorites
    {
        get => _isShowOnlyFavorites;
        set
        {
            this.RaiseAndSetIfChanged(ref _isShowOnlyFavorites, value);
            PerformSearch();
        }
    }

    public long BeatmapsCount { get; set; } = 0;

    private void PerformSearch()
        => _beatmapDataService.PerformSearch(SearchBeatmapSetText, IsShowOnlyFavorites);
}
